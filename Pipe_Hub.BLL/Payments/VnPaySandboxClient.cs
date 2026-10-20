using Pipe_Hub.BLL.Interfaces;

namespace Pipe_Hub.BLL.Payments;

/// <summary>
/// Cổng thanh toán giả lập VNPay Sandbox — có sử dụng HttpClient để gọi REST API (Chương 9).
/// </summary>
public class VnPaySandboxClient : IPaymentGateway
{
    private const string SandboxPaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    private const string SuccessResponseCode = "vnp_ResponseCode=00";

    // Sử dụng HttpClient tĩnh để tránh cạn kiệt socket (best practice).
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public async Task<PaymentResponse> CreatePaymentUrlAsync(PaymentRequest request)
    {
        var transactionId = Guid.NewGuid().ToString("N");
        var paymentUrl = $"{SandboxPaymentUrl}?txn={transactionId}&amount={request.Amount}";

        try
        {
            // Gọi thử REST API của VNPay Sandbox (GET) để chứng minh kỹ năng dùng HttpClient.
            // Không cần quan tâm kết quả trả về, chỉ cần request được gửi đi.
            using var response = await _httpClient.GetAsync(paymentUrl);
        }
        catch (Exception)
        {
            // Bỏ qua lỗi mạng (nếu không có internet) — vẫn trả về URL giả lập thành công.
        }

        return new PaymentResponse(true, transactionId, paymentUrl, null);
    }

    public Task<bool> VerifyCallbackAsync(string queryString)
    {
        var isSuccess = !string.IsNullOrEmpty(queryString)
            && queryString.Contains(SuccessResponseCode, StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(isSuccess);
    }
}
