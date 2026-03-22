using Messaging.Common.Events;
using ProductService.Contracts.Messaging;
namespace ProductService.Application.Messaging
{
    public class OrderPlacedHandler : IOrderPlacedHandler
    {
        public async Task HandleAsync(OrderPlacedEvent evt)
        {
            Console.WriteLine($"[ProductService] Reducing stock for Order: {evt.OrderNumber}");
            foreach (var item in evt.Items)
                Console.WriteLine($"  Product {item.ProductId}: -{item.Quantity} units");
            await Task.CompletedTask;
        }
    }
}
