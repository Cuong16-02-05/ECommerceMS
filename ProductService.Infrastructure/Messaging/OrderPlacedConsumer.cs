using Messaging.Common.Events;
using Messaging.Common.Options;
using Messaging.Common.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductService.Contracts.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
namespace ProductService.Infrastructure.Messaging
{
    public sealed class OrderPlacedConsumer : BackgroundService
    {
        private readonly ILogger<OrderPlacedConsumer> _logger;
        private readonly IModel _channel;
        private readonly RabbitMqOptions _opt;
        private readonly IServiceScopeFactory _scopeFactory;
        public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger,
            IModel channel, IOptions<RabbitMqOptions> opt, IServiceScopeFactory scopeFactory)
        {
            _logger = logger; _channel = channel;
            _opt = opt.Value; _scopeFactory = scopeFactory;
            RabbitTopology.EnsureAll(_channel, _opt);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel.BasicQos(0, 10, false);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<OrderPlacedEvent>(ea.Body.Span,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (evt == null) { _channel.BasicNack(ea.DeliveryTag, false, false); return; }
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IOrderPlacedHandler>();
                    await handler.HandleAsync(evt);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("[ProductService] Processed order {OrderId}", evt.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ProductService] Error");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };
            _channel.BasicConsume(_opt.ProductOrderPlacedQueue, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }
    }
}
