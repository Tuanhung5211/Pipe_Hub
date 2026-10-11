namespace Pipe_Hub.DAL.Entities;

public class LogEntry : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
}
