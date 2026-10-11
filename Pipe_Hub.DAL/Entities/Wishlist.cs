using System;

namespace Pipe_Hub.DAL.Entities;

public class Wishlist : BaseEntity
{
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
}
