using Messaging.Common.Events;
using NotificationService.Contracts.Messaging;
namespace NotificationService.Application.Messaging
{
    public class OrderPlacedHandler : IOrderPlacedHandler
    {
        public async Task HandleAsync(OrderPlacedEvent evt)
        {
            Console.WriteLine("=== NEW ORDER NOTIFICATION ===");
            Console.WriteLine($"Order:    {evt.OrderNumber}");
            Console.WriteLine($"Customer: {evt.CustomerName}");
            Console.WriteLine($"Email:    {evt.CustomerEmail}");
            Console.WriteLine($"Total:    {evt.TotalAmount:C}");
            Console.WriteLine($"TraceId:  {evt.CorrelationId}");
            Console.WriteLine("==============================");
            await Task.CompletedTask;
        }
    }
}
