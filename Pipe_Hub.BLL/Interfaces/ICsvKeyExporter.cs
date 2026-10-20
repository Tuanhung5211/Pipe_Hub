using Pipe_Hub.BLL.Common;

namespace Pipe_Hub.BLL.Interfaces;

public interface ICsvKeyExporter
{
    /// <summary>
    /// Export danh sách key của 1 game ra file CSV.
    /// File output có 5 cột: Key, Status, GameTitle, ActivatedAt, CreatedAt.
    /// </summary>
    /// <param name="gameId">Game cần export key</param>
    /// <param name="outputPath">Đường dẫn file CSV đích</param>
    /// <param name="statusFilter">Lọc theo status (Available/Sold/Activated). Null = export tất cả.</param>
    /// <param name="progress">Callback 0-100, có thể null</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result<int> với số dòng đã export</returns>
    Task<Result<int>> ExportAsync(
        int gameId,
        string outputPath,
        string? statusFilter = null,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}
