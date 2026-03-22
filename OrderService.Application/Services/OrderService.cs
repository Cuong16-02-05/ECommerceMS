using OrderService.Domain.Entities;
namespace OrderService.Application.Services
{
    public class OrderService : IOrderService
    {
        public async Task<Order> PlaceOrderAsync(Order order)
        {
            await Task.CompletedTask;
            return order;
        }
    }
}
