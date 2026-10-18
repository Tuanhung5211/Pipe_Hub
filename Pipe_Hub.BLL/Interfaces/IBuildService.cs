using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service quản lý GameBuild (bản phát hành) của game.
/// Chỉ Publisher chủ sở hữu game (hoặc Admin) mới thao tác được.
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// Lấy danh sách build của 1 game. Ném AppException nếu không có quyền.
    /// </summary>
    Task<IReadOnlyList<BuildDto>> GetBuildsByGameAsync(int gameId);

    /// <summary>
    /// Tạo build mới cho game. Chỉ Publisher chủ sở hữu (hoặc Admin).
    /// </summary>
    Task<Result<BuildDto>> CreateBuildAsync(int gameId, string version, string channel, string? patchNotes, string? filePath);

    /// <summary>
    /// Cập nhật build. Chỉ Publisher chủ sở hữu (hoặc Admin).
    /// </summary>
    Task<Result> UpdateBuildAsync(int buildId, string version, string channel, string? patchNotes, string? filePath);

    /// <summary>
    /// Xóa build (hard delete). Chỉ Publisher chủ sở hữu (hoặc Admin).
    /// </summary>
    Task<Result> DeleteBuildAsync(int buildId);
}
