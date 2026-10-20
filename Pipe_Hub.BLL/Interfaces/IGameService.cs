using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IGameService
{
    /// <summary>
    /// Lấy tất cả game chưa xóa. Admin/Publisher thấy hết; Player chỉ thấy Approved.
    /// </summary>
    Task<IReadOnlyList<GameDto>> GetAllVisibleAsync();

    /// <summary>
    /// Lấy game theo Id. Ném AppException nếu không tồn tại.
    /// </summary>
    Task<GameDto> GetByIdAsync(int id);

    /// <summary>
    /// Lấy game do Publisher X quản lý. Chỉ Publisher của game đó mới xem được.
    /// </summary>
    Task<IReadOnlyList<GameDto>> GetByPublisherAsync(int publisherId);

    /// <summary>
    /// Tạo game mới (draft). Chỉ Publisher.
    /// </summary>
    Task<Result<GameDto>> CreateAsync(string title, decimal price, string? description);

    /// <summary>
    /// Cập nhật game. Publisher chỉ sửa game của mình.
    /// </summary>
    Task<Result<GameDto>> UpdateAsync(int gameId, string title, decimal price, string? description);

    /// <summary>
    /// Cập nhật giá bán và giá sale theo thời gian. Admin hoặc Publisher chủ sở hữu.
    /// </summary>
    Task<Result> UpdatePricingAsync(int gameId, decimal price, decimal? salePrice, DateTime? saleStart, DateTime? saleEnd);

    /// <summary>
    /// Cập nhật trang game (mô tả dài + ảnh bìa). Admin hoặc Publisher chủ sở hữu.
    /// </summary>
    Task<Result> UpdateStorePageAsync(int gameId, string? description, string? coverImagePath);

    /// <summary>
    /// Gửi duyệt game (Draft → Pending).
    /// </summary>
    Task<Result> SubmitForApprovalAsync(int gameId);

    /// <summary>
    /// Duyệt game (Pending → Approved). Chỉ Admin.
    /// </summary>
    Task<Result> ApproveAsync(int gameId);

    /// <summary>
    /// Từ chối game (Pending → Rejected). Chỉ Admin.
    /// </summary>
    Task<Result> RejectAsync(int gameId);

    /// <summary>
    /// Soft delete game. Publisher chỉ xóa game của mình.
    /// </summary>
    Task<Result> SoftDeleteAsync(int gameId);
}
