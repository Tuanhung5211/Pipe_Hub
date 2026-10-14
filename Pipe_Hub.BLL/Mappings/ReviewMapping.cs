using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Mappings;

public static class ReviewMapping
{
    public static ReviewDto ToDto(this Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            Username = review.User?.Username ?? string.Empty,
            FullName = review.User?.FullName ?? string.Empty,
            GameId = review.GameId,
            GameTitle = review.Game?.Title ?? string.Empty,
            Rating = review.Rating,
            Content = review.Content,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}
