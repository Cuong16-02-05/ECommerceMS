using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
namespace OrderService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService) => _orderService = orderService;
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderRequest req)
        {
            var order = new Order
            {
                UserId = req.UserId,
                CustomerName = req.CustomerName,
                CustomerEmail = req.CustomerEmail,
                PhoneNumber = req.PhoneNumber,
                TotalAmount = req.TotalAmount,
                Items = req.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
            var result = await _orderService.PlaceOrderAsync(order);
            return Ok(new { message = "Order placed!", orderId = result.Id, orderNumber = result.OrderNumber });
        }
    }
    public record CreateOrderRequest(Guid UserId, string CustomerName, string CustomerEmail,
        string PhoneNumber, decimal TotalAmount, List<OrderItemRequest> Items);
    public record OrderItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);
}
