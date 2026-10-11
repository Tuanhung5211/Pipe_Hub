namespace Pipe_Hub.DAL.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public int? ReviewId { get; set; }
    public Review? Review { get; set; }
}
