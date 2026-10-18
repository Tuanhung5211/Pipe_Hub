namespace Pipe_Hub.BLL.DTOs;

public record RoleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
