using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Contracts.Interfaces;
namespace NotificationService.Infrastructure.BackgroundJobs
{
    public sealed class NotificationWorker : BackgroundService
    {
        private readonly ILogger<NotificationWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
        public NotificationWorker(ILogger<NotificationWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger; _scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[NotificationWorker] Started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
                    await processor.ProcessQueueBatchAsync(50, 0);
                }
                catch (Exception ex) { _logger.LogError(ex, "NotificationWorker error"); }
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
