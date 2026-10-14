using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IKeyService
{
    /// <summary>
    /// Sinh N key mới cho game (Publisher sở hữu game hoặc Admin).
    /// Trả về danh sách key đã sinh.
    /// </summary>
    Task<Result<IReadOnlyList<GameKeyDto>>> GenerateKeysAsync(int gameId, int count);

    /// <summary>
    /// Lấy danh sách key của 1 game (Publisher sở hữu hoặc Admin).
    /// </summary>
    Task<IReadOnlyList<GameKeyDto>> GetKeysByGameAsync(int gameId);

    /// <summary>
    /// Sinh QR code cho 1 key — trả về byte[] PNG.
    /// </summary>
    Task<byte[]> GenerateQrCodeAsync(int keyId);

    /// <summary>
    /// Đếm số key Available còn lại cho game.
    /// </summary>
    Task<int> CountAvailableAsync(int gameId);
}
