using System.Collections.Generic;

namespace Pipe_Hub.DAL.Entities;

public class Review : BaseEntity
{
    public int Rating { get; set; }
    public string? Content { get; set; }
    public bool IsDeleted { get; set; } = false;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
