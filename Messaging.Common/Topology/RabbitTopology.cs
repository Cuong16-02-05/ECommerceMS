using Messaging.Common.Options;
using RabbitMQ.Client;
namespace Messaging.Common.Topology
{
    public static class RabbitTopology
    {
        public static void EnsureAll(IModel channel, RabbitMqOptions opt)
        {
            channel.ExchangeDeclare(opt.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            if (!string.IsNullOrWhiteSpace(opt.DlxExchangeName))
            {
                channel.ExchangeDeclare(opt.DlxExchangeName!, ExchangeType.Fanout, durable: true, autoDelete: false);
                if (!string.IsNullOrWhiteSpace(opt.DlxQueueName))
                {
                    channel.QueueDeclare(opt.DlxQueueName!, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.QueueBind(opt.DlxQueueName, opt.DlxExchangeName!, routingKey: "");
                }
            }
            var args = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(opt.DlxExchangeName))
                args["x-dead-letter-exchange"] = opt.DlxExchangeName;
            channel.QueueDeclare(opt.ProductOrderPlacedQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);
            channel.QueueDeclare(opt.NotificationOrderPlacedQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);
            channel.QueueBind(opt.ProductOrderPlacedQueue, opt.ExchangeName, routingKey: "order.placed");
            channel.QueueBind(opt.NotificationOrderPlacedQueue, opt.ExchangeName, routingKey: "order.placed");
        }
    }
}
