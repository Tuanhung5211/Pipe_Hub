namespace Pipe_Hub.BLL.DTOs;

public record LibraryItemDto
{
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string PublisherName { get; init; } = string.Empty;
    public string? CoverImagePath { get; init; }
    public decimal PurchasePrice { get; init; }
    public DateTime PurchasedAt { get; init; }
    public int TotalPlayMinutes { get; init; }
}
