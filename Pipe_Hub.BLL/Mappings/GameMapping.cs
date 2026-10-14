using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Mappings;

public static class GameMapping
{
    public static GameDto ToDto(this Game game)
    {
        return new GameDto
        {
            Id = game.Id,
            Title = game.Title,
            Description = game.Description,
            CoverImagePath = game.CoverImagePath,
            Price = game.Price,
            SalePrice = game.SalePrice,
            SaleStartAt = game.SaleStartAt,
            SaleEndAt = game.SaleEndAt,
            Status = game.Status.ToString(),
            IsDeleted = game.IsDeleted,
            PublisherId = game.PublisherId,
            PublisherName = game.Publisher?.FullName ?? string.Empty,
            CreatedAt = game.CreatedAt,
            GenreNames = game.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
            TagNames = game.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            LanguageNames = game.Languages?.Select(l => l.Name).ToList() ?? new List<string>()
        };
    }
}
