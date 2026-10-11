using System;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Entities;

public class GameKey : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public KeyStatus Status { get; set; } = KeyStatus.Available;
    public DateTime? ActivatedAt { get; set; }

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}
