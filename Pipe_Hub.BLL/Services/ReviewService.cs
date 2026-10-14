using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

public class ReviewService : IReviewService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public ReviewService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetByGameAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var reviews = await context.Reviews
            .Include(r => r.User)
            .Include(r => r.Game)
            .Where(r => r.GameId == gameId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Select(r => r.ToDto()).ToList();
    }

    public async Task<ReviewDto?> GetMyReviewForGameAsync(int gameId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var review = await context.Reviews
            .Include(r => r.User)
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.GameId == gameId
                                   && r.UserId == _currentUser.UserId.Value);

        return review?.ToDto();
    }

    public async Task<Result<ReviewDto>> CreateAsync(int gameId, int rating, string? content)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (rating < 1 || rating > 5)
        {
            return Result<ReviewDto>.Failure("Điểm đánh giá phải từ 1 đến 5.");
        }

        if (content is not null && content.Length > 2000)
        {
            return Result<ReviewDto>.Failure("Nội dung đánh giá tối đa 2000 ký tự.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var hasPurchased = await context.GameKeys
            .AnyAsync(k => k.GameId == gameId
                        && k.Status == KeyStatus.Activated
                        && k.Order != null
                        && k.Order.UserId == userId);

        if (!hasPurchased)
        {
            return Result<ReviewDto>.Failure("Bạn cần mua game trước khi đánh giá.");
        }

        var existing = await context.Reviews
            .AnyAsync(r => r.GameId == gameId && r.UserId == userId);

        if (existing)
        {
            return Result<ReviewDto>.Failure("Bạn đã đánh giá game này rồi.");
        }

        var review = new Review
        {
            UserId = userId,
            GameId = gameId,
            Rating = rating,
            Content = content
        };

        context.Reviews.Add(review);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateReview",
            details: $"User {_currentUser.Username} đánh giá game Id = {gameId} ({rating}/5)",
            userId: userId,
            entityName: nameof(Review),
            entityId: review.Id);

        var fresh = await context.Reviews
            .Include(r => r.User)
            .Include(r => r.Game)
            .FirstAsync(r => r.Id == review.Id);

        return Result<ReviewDto>.Success(fresh.ToDto());
    }

    public async Task<Result<ReviewDto>> UpdateAsync(int reviewId, int rating, string? content)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (rating < 1 || rating > 5)
        {
            return Result<ReviewDto>.Failure("Điểm đánh giá phải từ 1 đến 5.");
        }

        if (content is not null && content.Length > 2000)
        {
            return Result<ReviewDto>.Failure("Nội dung đánh giá tối đa 2000 ký tự.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var review = await context.Reviews
            .Include(r => r.User)
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review is null)
        {
            return Result<ReviewDto>.Failure($"Không tìm thấy đánh giá Id = {reviewId}.");
        }

        if (review.UserId != userId)
        {
            throw new AppException("Bạn không có quyền sửa đánh giá này.");
        }

        review.Rating = rating;
        review.Content = content;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateReview",
            details: $"User {_currentUser.Username} sửa đánh giá Id = {reviewId} ({rating}/5)",
            userId: userId,
            entityName: nameof(Review),
            entityId: reviewId);

        return Result<ReviewDto>.Success(review.ToDto());
    }

    public async Task<Result> SoftDeleteAsync(int reviewId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var review = await context.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review is null)
        {
            return Result.Failure($"Không tìm thấy đánh giá Id = {reviewId}.");
        }

        if (review.UserId != userId && !_currentUser.IsAdmin)
        {
            throw new AppException("Bạn không có quyền xóa đánh giá này.");
        }

        review.IsDeleted = true;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "DeleteReview",
            details: $"User {_currentUser.Username} xóa đánh giá Id = {reviewId}",
            userId: userId,
            entityName: nameof(Review),
            entityId: reviewId);

        return Result.Success();
    }

    public async Task<double> GetAverageRatingAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Reviews.Where(r => r.GameId == gameId);

        if (!await query.AnyAsync())
        {
            return 0;
        }

        return await query.AverageAsync(r => r.Rating);
    }

    public async Task<int> GetReviewCountAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Reviews.CountAsync(r => r.GameId == gameId);
    }

    public async Task<bool> HasPurchasedGameAsync(int gameId)
    {
        if (!_currentUser.UserId.HasValue) return false;

        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.GameKeys
            .AnyAsync(k => k.GameId == gameId
                && k.Status == KeyStatus.Activated
                && k.Order != null
                && k.Order.UserId == _currentUser.UserId.Value);
    }
}
