using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface ILibraryService
{
    /// <summary>
    /// Lấy danh sách game user hiện tại đã mua + tổng playtime.
    /// </summary>
    Task<IReadOnlyList<LibraryItemDto>> GetMyLibraryAsync();

    /// <summary>
    /// Tổng thời gian chơi (phút) của user hiện tại cho 1 game.
    /// </summary>
    Task<int> GetTotalPlaytimeAsync(int gameId);
}
