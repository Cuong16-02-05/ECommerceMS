namespace Messaging.Common.Options
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string ExchangeName { get; set; } = "ecommerce.topic";
        public string? DlxExchangeName { get; set; } = "ecommerce.dlx";
        public string? DlxQueueName { get; set; } = "ecommerce.dlq";
        public string ProductOrderPlacedQueue { get; set; } = "product.order_placed";
        public string NotificationOrderPlacedQueue { get; set; } = "notification.order_placed";
    }
}
