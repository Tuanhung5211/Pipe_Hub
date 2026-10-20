using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service đọc/ghi SystemConfig.
/// Cache trong RAM — chỉ load 1 lần từ DB.
/// </summary>
public interface ISystemConfigService
{
    /// <summary>
    /// Lấy value theo key. Trả về null nếu không tồn tại.
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Lấy value kiểu decimal (cho CommissionRate). Trả về default nếu không parse được.
    /// </summary>
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0m);

    /// <summary>
    /// Lấy value kiểu int. Trả về default nếu không parse được.
    /// </summary>
    Task<int> GetIntAsync(string key, int defaultValue = 0);

    /// <summary>
    /// Set value cho key. Nếu key chưa có → tạo mới.
    /// </summary>
    Task SetAsync(string key, string value, string? description = null);

    /// <summary>Lấy toàn bộ config (không qua cache — query DB trực tiếp).</summary>
    Task<IReadOnlyList<SystemConfigDto>> GetAllAsync();

    /// <summary>
    /// Xóa cache — gọi khi cần reload từ DB.
    /// </summary>
    void InvalidateCache();
}
