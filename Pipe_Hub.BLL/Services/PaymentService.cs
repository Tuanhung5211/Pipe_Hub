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

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Bắt buộc phải có tham số txn (mã giao dịch) trên callback.
        var topUpRequestId = ExtractTopUpRequestId(queryString);

        if (!topUpRequestId.HasValue)
        {
            return Result.Failure("Thiếu mã giao dịch.");
        }

        var topUp = await context.TopUpRequests
            .FirstOrDefaultAsync(t => t.Id == topUpRequestId.Value);

        if (topUp is null)
        {
            // Callback hợp lệ nhưng không gắn với yêu cầu nạp nào (VD: thanh toán gói Marketing).
            _ = _logService.WriteAsync(
                action: "ProcessPaymentCallback",
                details: "Nhận callback thanh toán thành công (không có yêu cầu nạp tiền tương ứng).",
                userId: _currentUser.UserId);

            return Result.Success();
        }

        if (topUp.Status != TopUpStatus.Pending)
        {
            // Callback lặp lại → không cộng tiền lần hai.
            _ = _logService.WriteAsync(
                action: "ProcessPaymentCallback",
                details: $"Bỏ qua callback lặp cho yêu cầu nạp #{topUp.Id} (Status = {topUp.Status}).",
                userId: _currentUser.UserId,
                entityName: nameof(TopUpRequest),
                entityId: topUp.Id);

            return Result.Success();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var wallet = await context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == topUp.UserId);

            if (wallet is null)
            {
                throw new AppException($"Không tìm thấy ví của user Id = {topUp.UserId}.");
            }

            // Cộng Balance
            wallet.Balance += topUp.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Tạo Transaction TopUp (Amount DƯƠNG)
            context.Transactions.Add(new Transaction
            {
                UserId = topUp.UserId,
                WalletId = wallet.Id,
                Amount = topUp.Amount,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                Description = $"Thanh toán VNPay Sandbox - yêu cầu #{topUp.Id}"
            });

            // Cập nhật TopUpRequest
            topUp.Status = TopUpStatus.Approved;
            topUp.ApprovedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _ = _logService.WriteAsync(
                action: "ProcessPaymentCallback",
                details: $"Callback thanh toán thành công: cộng {topUp.Amount:N0} cho UserId={topUp.UserId} (yêu cầu #{topUp.Id}).",
                userId: _currentUser.UserId,
                entityName: nameof(TopUpRequest),
                entityId: topUp.Id);

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion conflict — callback trùng hoặc Admin duyệt cùng lúc
            await transaction.RollbackAsync();
            return Result.Failure(
                "Giao dịch bị xung đột do có thao tác đồng thời. Vui lòng thử lại.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Lấy tham số <c>txn</c> (Id yêu cầu nạp) từ query string của callback.
    /// Trả về <c>null</c> nếu không có hoặc không phải số nguyên hợp lệ.
    /// </summary>
    private static int? ExtractTopUpRequestId(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        foreach (var pair in queryString.TrimStart('?')
                     .Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);

            if (parts.Length == 2
                && string.Equals(parts[0], "txn", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out var id))
            {
                return id;
            }
        }

        return null;
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
