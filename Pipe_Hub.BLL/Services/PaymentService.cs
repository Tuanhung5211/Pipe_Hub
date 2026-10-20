using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Điều phối thanh toán qua cổng VNPay Sandbox: tạo URL thanh toán cho
/// yêu cầu nạp tiền và xử lý callback trả về.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly IPaymentGateway _gateway;
    private readonly ILogService _logService;
    private readonly CurrentUserContext _currentUser;

    public PaymentService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        IPaymentGateway gateway,
        ILogService logService,
        CurrentUserContext currentUser)
    {
        _contextFactory = contextFactory;
        _gateway = gateway;
        _logService = logService;
        _currentUser = currentUser;
    }

    public async Task<Result<string>> CreateTopUpPaymentUrlAsync(int topUpRequestId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var topUp = await context.TopUpRequests
            .FirstOrDefaultAsync(t => t.Id == topUpRequestId);

        if (topUp is null || topUp.UserId != _currentUser.UserId.Value)
        {
            throw new AppException("Không tìm thấy yêu cầu nạp tiền của bạn.");
        }

        if (topUp.Status != TopUpStatus.Pending)
        {
            throw new AppException("Yêu cầu nạp tiền này đã được xử lý.");
        }

        var response = await _gateway.CreatePaymentUrlAsync(
            new PaymentRequest(topUp.Id.ToString(), topUp.Amount, "Nạp tiền vào ví"));

        if (!response.IsSuccess)
        {
            return Result<string>.Failure(
                response.ErrorMessage ?? "Không thể tạo URL thanh toán.");
        }

        _ = _logService.WriteAsync(
            action: "CreateTopUpPaymentUrl",
            details: $"User tạo URL thanh toán cho yêu cầu nạp {topUp.Amount:N0} (Id={topUp.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(TopUpRequest),
            entityId: topUp.Id);

        return Result<string>.Success(response.PaymentUrl);
    }

    public async Task<Result> ProcessCallbackAsync(string queryString)
    {
        var isSuccess = await _gateway.VerifyCallbackAsync(queryString);

        if (!isSuccess)
        {
            return Result.Failure("Chữ ký không hợp lệ hoặc giao dịch thất bại.");
        }

        _ = _logService.WriteAsync(
            action: "ProcessPaymentCallback",
            details: "Nhận callback thanh toán thành công từ VNPay Sandbox.",
            userId: _currentUser.UserId);

        return Result.Success();
    }

    public async Task<Result<string>> CreateMarketingPaymentUrlAsync(string packageName, decimal amount)
    {
        if (!_currentUser.IsPublisher)
        {
            throw new AppException("Chỉ Publisher mới có quyền mua gói Marketing.");
        }

        var response = await _gateway.CreatePaymentUrlAsync(
            new PaymentRequest($"MKT-{Guid.NewGuid():N}", amount, $"Mua gói {packageName}"));

        if (!response.IsSuccess)
        {
            return Result<string>.Failure(response.ErrorMessage ?? "Lỗi tạo URL.");
        }

        _ = _logService.WriteAsync(
            action: "CreateMarketingPayment",
            details: $"Publisher mua gói {packageName} - {amount:N0}",
            userId: _currentUser.UserId);

        return Result<string>.Success(response.PaymentUrl);
    }
}
