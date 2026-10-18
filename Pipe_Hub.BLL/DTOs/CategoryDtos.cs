namespace Pipe_Hub.BLL.DTOs;

/// <summary>DTO cho Genre (thể loại game).</summary>
public record GenreDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>DTO cho Tag (thẻ game).</summary>
public record TagDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>DTO cho Language (ngôn ngữ hỗ trợ).</summary>
public record LanguageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
}
