using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetByGameAsync(int gameId);
    Task<ReviewDto?> GetMyReviewForGameAsync(int gameId);
    Task<Result<ReviewDto>> CreateAsync(int gameId, int rating, string? content);
    Task<Result<ReviewDto>> UpdateAsync(int reviewId, int rating, string? content);
    Task<Result> SoftDeleteAsync(int reviewId);
    Task<double> GetAverageRatingAsync(int gameId);
    Task<int> GetReviewCountAsync(int gameId);

    /// <summary>
    /// Kiểm tra user hiện tại đã mua game chưa (có GameKey Activated thuộc Order của user).
    /// </summary>
    Task<bool> HasPurchasedGameAsync(int gameId);
}
