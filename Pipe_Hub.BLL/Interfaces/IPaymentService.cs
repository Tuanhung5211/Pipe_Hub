using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IPaymentService
{
    /// <summary>Khởi tạo thanh toán cho 1 đơn nạp tiền (TopUpRequest) đã có.</summary>
    Task<Result<string>> CreateTopUpPaymentUrlAsync(int topUpRequestId);

    /// <summary>Xử lý callback từ VNPay (cập nhật DB nếu thành công).</summary>
    Task<Result> ProcessCallbackAsync(string queryString);

    /// <summary>Khởi tạo thanh toán cho gói Marketing (không qua DB).</summary>
    Task<Result<string>> CreateMarketingPaymentUrlAsync(string packageName, decimal amount);
}
