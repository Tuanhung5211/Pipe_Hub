using System;

namespace Pipe_Hub.DAL.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
