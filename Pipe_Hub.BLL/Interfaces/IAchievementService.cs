using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service quản lý Achievement (thành tích) của game.
/// Chỉ Admin hoặc Publisher sở hữu game mới được CRUD.
/// </summary>
public interface IAchievementService
{
    Task<IReadOnlyList<AchievementDto>> GetByGameAsync(int gameId);

    Task<Result<AchievementDto>> CreateAsync(int gameId, string name, string? description, string? iconPath);

    Task<Result> UpdateAsync(int id, string name, string? description, string? iconPath);

    Task<Result> DeleteAsync(int id);

    /// <summary>Lấy danh sách thành tích user hiện tại đã đạt được.</summary>
    Task<IReadOnlyList<PlayerAchievementDto>> GetMyAchievementsAsync();
}
