using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service ghi/đọc LogEntry — audit nghiệp vụ.
/// KHÁC với Serilog (log kỹ thuật). LogService dùng cho LogViewerForm.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Ghi 1 log entry. Không throw — nếu ghi log fail thì bỏ qua.
    /// </summary>
    Task WriteAsync(string action, string? details = null, int? userId = null,
                    string? entityName = null, int? entityId = null);

    /// <summary>
    /// Lấy N log gần nhất, mới nhất trước.
    /// </summary>
    Task<List<LogEntry>> GetRecentAsync(int count = 100);

    /// <summary>
    /// Lấy log của 1 user.
    /// </summary>
    Task<List<LogEntry>> GetByUserAsync(int userId, int count = 50);
}
