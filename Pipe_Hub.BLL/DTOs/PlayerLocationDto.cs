namespace Pipe_Hub.BLL.DTOs;

public record PlayerLocationDto
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Country { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
