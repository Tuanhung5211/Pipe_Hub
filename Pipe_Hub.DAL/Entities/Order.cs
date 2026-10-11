using System;
using System.Collections.Generic;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Entities;

public class Order : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? Note { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<GameKey> Keys { get; set; } = new List<GameKey>();
}
