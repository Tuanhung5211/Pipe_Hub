using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

public class WalletService : IWalletService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public WalletService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<WalletDto> GetMyWalletAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var wallet = await context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.UserId == _currentUser.UserId.Value);

        if (wallet is null)
        {
            throw new AppException("Không tìm thấy ví của bạn.");
        }

        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Username = wallet.User?.Username ?? string.Empty,
            Balance = wallet.Balance,
            CreatedAt = wallet.CreatedAt
        };
    }

    public async Task<Result<TopUpRequestDto>> CreateTopUpRequestAsync(
        decimal amount,
        string? paymentMethod,
        string? paymentReference)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (amount <= 0)
        {
            return Result<TopUpRequestDto>.Failure("Số tiền nạp phải lớn hơn 0.");
        }

        if (amount < 10_000)
        {
            return Result<TopUpRequestDto>.Failure("Số tiền nạp tối thiểu là 10,000.");
        }

        if (amount > 50_000_000)
        {
            return Result<TopUpRequestDto>.Failure("Số tiền nạp tối đa là 50,000,000.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!string.IsNullOrWhiteSpace(paymentReference))
        {
            var referenceExists = await context.TopUpRequests
                .AnyAsync(t => t.PaymentReference == paymentReference);

            if (referenceExists)
            {
                return Result<TopUpRequestDto>.Failure("Mã giao dịch đã tồn tại.");
            }
        }

        var request = new TopUpRequest
        {
            UserId = _currentUser.UserId.Value,
            Amount = amount,
            Status = TopUpStatus.Pending,
            PaymentMethod = paymentMethod,
            PaymentReference = paymentReference
        };

        context.TopUpRequests.Add(request);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateTopUpRequest",
            details: $"User tạo yêu cầu nạp {amount:N0} (Id={request.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(TopUpRequest),
            entityId: request.Id);

        return Result<TopUpRequestDto>.Success(new TopUpRequestDto
        {
            Id = request.Id,
            UserId = request.UserId,
            Amount = request.Amount,
            Status = request.Status.ToString(),
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            CreatedAt = request.CreatedAt
        });
    }

    public async Task<IReadOnlyList<TopUpRequestDto>> GetMyTopUpRequestsAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var items = await context.TopUpRequests
            .Include(t => t.User)
            .Where(t => t.UserId == _currentUser.UserId.Value)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return items.Select(t => new TopUpRequestDto
        {
            Id = t.Id,
            UserId = t.UserId,
            Username = t.User?.Username ?? string.Empty,
            Amount = t.Amount,
            Status = t.Status.ToString(),
            PaymentMethod = t.PaymentMethod,
            PaymentReference = t.PaymentReference,
            ApprovedAt = t.ApprovedAt,
            RejectReason = t.RejectReason,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<IReadOnlyList<TopUpRequestDto>> GetAllTopUpRequestsAsync(string? statusFilter = null)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới xem được tất cả yêu cầu nạp.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.TopUpRequests.Include(t => t.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<TopUpStatus>(statusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        var items = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        return items.Select(t => new TopUpRequestDto
        {
            Id = t.Id,
            UserId = t.UserId,
            Username = t.User?.Username ?? string.Empty,
            Amount = t.Amount,
            Status = t.Status.ToString(),
            PaymentMethod = t.PaymentMethod,
            PaymentReference = t.PaymentReference,
            ApprovedAt = t.ApprovedAt,
            RejectReason = t.RejectReason,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<Result> ApproveTopUpAsync(int topUpRequestId)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới được duyệt yêu cầu nạp.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var request = await context.TopUpRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == topUpRequestId);

            if (request is null)
            {
                throw new AppException($"Không tìm thấy yêu cầu nạp Id = {topUpRequestId}.");
            }

            // Chỉ cập nhật khi yêu cầu vẫn còn Pending → chặn 2 Admin duyệt cùng lúc
            var rows = await context.TopUpRequests
                .Where(t => t.Id == topUpRequestId && t.Status == TopUpStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Status, TopUpStatus.Approved)
                    .SetProperty(t => t.ApprovedAt, DateTime.UtcNow));

            if (rows == 0)
            {
                await transaction.RollbackAsync();
                return Result.Failure("Yêu cầu đã được xử lý hoặc không tồn tại.");
            }

            var wallet = await context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.UserId);

            if (wallet is null)
            {
                throw new AppException($"Không tìm thấy ví của user Id = {request.UserId}.");
            }

            // Cộng Balance
            wallet.Balance += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Tạo Transaction TopUp (Amount DƯƠNG)
            var txn = new Transaction
            {
                UserId = request.UserId,
                WalletId = wallet.Id,
                Amount = request.Amount,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                Description = $"Duyệt nạp ví - yêu cầu #{request.Id}"
            };
            context.Transactions.Add(txn);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            _ = _logService.WriteAsync(
                action: "ApproveTopUp",
                details: $"Admin duyệt nạp {request.Amount:N0} cho UserId={request.UserId}",
                userId: _currentUser.UserId,
                entityName: nameof(TopUpRequest),
                entityId: request.Id);

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion conflict — 2 Admin duyệt cùng lúc
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

    public async Task<Result> RejectTopUpAsync(int topUpRequestId, string reason)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới được từ chối yêu cầu nạp.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Vui lòng nhập lý do từ chối.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var request = await context.TopUpRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == topUpRequestId);

        if (request is null)
        {
            throw new AppException($"Không tìm thấy yêu cầu nạp Id = {topUpRequestId}.");
        }

        // Chỉ cập nhật khi yêu cầu vẫn còn Pending → chặn 2 Admin xử lý cùng lúc
        var trimmedReason = reason.Trim();
        var rows = await context.TopUpRequests
            .Where(t => t.Id == topUpRequestId && t.Status == TopUpStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, TopUpStatus.Rejected)
                .SetProperty(t => t.RejectReason, trimmedReason));

        if (rows == 0)
        {
            return Result.Failure("Yêu cầu đã được xử lý hoặc không tồn tại.");
        }

        _ = _logService.WriteAsync(
            action: "RejectTopUp",
            details: $"Admin từ chối yêu cầu nạp #{request.Id} - lý do: {reason}",
            userId: _currentUser.UserId,
            entityName: nameof(TopUpRequest),
            entityId: request.Id);

        return Result.Success();
    }
}
