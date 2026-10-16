namespace Pipe_Hub.BLL.DTOs;

public record GameDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CoverImagePath { get; init; }
    public decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public DateTime? SaleStartAt { get; init; }
    public DateTime? SaleEndAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public int PublisherId { get; init; }
    public string PublisherName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> GenreNames { get; init; } = new List<string>();
    public IReadOnlyList<string> TagNames { get; init; } = new List<string>();
    public IReadOnlyList<string> LanguageNames { get; init; } = new List<string>();

    public string GenresText => string.Join(", ", GenreNames);
}
