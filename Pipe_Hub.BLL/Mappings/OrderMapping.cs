using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Mappings;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            UserId = order.UserId,
            Username = order.User?.Username ?? string.Empty,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            Items = order.Items?.Select(i => i.ToDto()).ToList() ?? new List<OrderItemDto>(),
            GameKeys = order.Keys?.Select(k => k.Key).ToList() ?? new List<string>()
        };
    }

    public static OrderItemDto ToDto(this OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
            GameId = item.GameId,
            GameTitle = item.Game?.Title ?? string.Empty,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Subtotal = item.Quantity * item.UnitPrice
        };
    }
}
