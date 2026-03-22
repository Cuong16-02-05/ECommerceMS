using OrderService.Domain.Entities;
namespace OrderService.Application.Services
{
    public interface IOrderService
    {
        Task<Order> PlaceOrderAsync(Order order);
    }
}
