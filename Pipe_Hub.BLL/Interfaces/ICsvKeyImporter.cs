using Pipe_Hub.BLL.Common;

namespace Pipe_Hub.BLL.Interfaces;

public interface ICsvKeyImporter
{
    /// <summary>
    /// Import key từ file CSV vào game.
    /// File CSV phải có cột header tên "Key" (không phân biệt hoa/thường).
    /// Key trùng (đã có trong DB) hoặc không hợp lệ sẽ bị bỏ qua.
    /// </summary>
    /// <param name="gameId">Game nhận key</param>
    /// <param name="filePath">Đường dẫn file CSV</param>
    /// <param name="progress">Callback báo tiến độ (0-100), có thể null</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result<int> với số key đã import thành công</returns>
    Task<Result<int>> ImportAsync(
        int gameId,
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}
