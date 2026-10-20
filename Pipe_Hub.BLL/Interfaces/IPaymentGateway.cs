namespace Pipe_Hub.BLL.Interfaces;

public record PaymentRequest(string OrderCode, decimal Amount, string Description);

public record PaymentResponse(bool IsSuccess, string TransactionId, string PaymentUrl, string? ErrorMessage);

public interface IPaymentGateway
{
    /// <summary>Khởi tạo giao dịch và lấy URL thanh toán.</summary>
    Task<PaymentResponse> CreatePaymentUrlAsync(PaymentRequest request);

    /// <summary>Xác thực chữ ký và xử lý callback từ cổng thanh toán.</summary>
    Task<bool> VerifyCallbackAsync(string queryString);
}
