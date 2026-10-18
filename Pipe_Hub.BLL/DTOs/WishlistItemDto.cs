namespace Pipe_Hub.BLL.DTOs;

public record WishlistItemDto
{
    public int Id { get; init; }
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string PublisherName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public string? CoverImagePath { get; init; }
    public DateTime AddedAt { get; init; }
}
