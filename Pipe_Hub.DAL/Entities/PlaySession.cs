using System;

namespace Pipe_Hub.DAL.Entities;

public class PlaySession : BaseEntity
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
}
