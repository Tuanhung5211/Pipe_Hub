using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IWalletService
{
    /// <summary>Lấy ví của user hiện tại (Player chỉ xem ví mình).</summary>
    Task<WalletDto> GetMyWalletAsync();

    /// <summary>Player tạo yêu cầu nạp ví — KHÔNG cộng Balance ngay.</summary>
    Task<Result<TopUpRequestDto>> CreateTopUpRequestAsync(decimal amount, string? paymentMethod, string? paymentReference);

    /// <summary>Lấy danh sách yêu cầu nạp của user hiện tại.</summary>
    Task<IReadOnlyList<TopUpRequestDto>> GetMyTopUpRequestsAsync();

    /// <summary>Admin: lấy tất cả yêu cầu nạp (filter theo status optional).</summary>
    Task<IReadOnlyList<TopUpRequestDto>> GetAllTopUpRequestsAsync(string? statusFilter = null);

    /// <summary>Admin duyệt yêu cầu nạp → cộng Balance + tạo Transaction trong 1 transaction.</summary>
    Task<Result> ApproveTopUpAsync(int topUpRequestId);

    /// <summary>Admin từ chối yêu cầu nạp.</summary>
    Task<Result> RejectTopUpAsync(int topUpRequestId, string reason);
}
