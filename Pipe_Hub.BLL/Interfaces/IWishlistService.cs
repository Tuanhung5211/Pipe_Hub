using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IWishlistService
{
    /// <summary>Lấy wishlist của user hiện tại (game chưa xóa).</summary>
    Task<IReadOnlyList<WishlistItemDto>> GetMyWishlistAsync();

    /// <summary>Kiểm tra game có trong wishlist của user hiện tại không.</summary>
    Task<bool> IsInWishlistAsync(int gameId);

    /// <summary>Thêm game vào wishlist. Báo lỗi nếu đã có.</summary>
    Task<Result> AddAsync(int gameId);

    /// <summary>Xóa game khỏi wishlist của user hiện tại.</summary>
    Task<Result> RemoveAsync(int gameId);
}
