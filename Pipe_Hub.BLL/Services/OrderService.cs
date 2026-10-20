using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;
using Pipe_Hub.DAL.Repositories;

namespace Pipe_Hub.BLL.Services;

public class OrderService : IOrderService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;
    private readonly IEmailService _emailService;

    public OrderService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService,
        IEmailService emailService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
        _emailService = emailService;
    }

    public async Task<Result<OrderDto>> PurchaseAsync(int gameId, int quantity = 1)
    {
        // Validate cơ bản
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (quantity < 1)
        {
            return Result<OrderDto>.Failure("Số lượng phải ít nhất là 1.");
        }

        if (quantity > 1)
        {
            return Result<OrderDto>.Failure("Chưa hỗ trợ mua nhiều hơn 1 bản mỗi game.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var uow = new UnitOfWork(context);
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. Load game
            var game = await context.Games
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game is null)
            {
                return Result<OrderDto>.Failure($"Không tìm thấy game với Id = {gameId}.");
            }

            if (game.IsDeleted)
            {
                return Result<OrderDto>.Failure("Game không còn được bán.");
            }

            if (game.Status != GameStatus.Approved)
            {
                return Result<OrderDto>.Failure("Game chưa được duyệt hoặc đã bị ẩn.");
            }

            // 2. Check user đã mua game này chưa (có key Activated không)
            var hasPurchased = await context.GameKeys
                .AnyAsync(k => k.GameId == gameId
                            && k.Status == KeyStatus.Activated
                            && k.Order != null
                            && k.Order.UserId == userId);

            if (hasPurchased)
            {
                return Result<OrderDto>.Failure("Bạn đã sở hữu game này rồi.");
            }

            // 3. Tính giá (Sale nếu đang trong thời gian)
            var now = DateTime.UtcNow;
            var isOnSale = game.SalePrice.HasValue
                        && game.SaleStartAt.HasValue
                        && game.SaleEndAt.HasValue
                        && game.SaleStartAt.Value <= now
                        && game.SaleEndAt.Value >= now;

            var unitPrice = isOnSale ? game.SalePrice!.Value : game.Price;
            var totalAmount = unitPrice * quantity;

            // 4. Load wallet
            var wallet = await context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet is null)
            {
                throw new AppException("Không tìm thấy ví của bạn.");
            }

            if (wallet.Balance < totalAmount)
            {
                return Result<OrderDto>.Failure(
                    $"Số dư ví không đủ. Cần {totalAmount:N0}, hiện có {wallet.Balance:N0}.");
            }

            // 5. Tạo Order (trước khi lấy key để có order.Id cho update atomic)
            var orderCode = $"ORD-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                TotalAmount = totalAmount,
                Status = OrderStatus.Completed,
                Note = $"Mua '{game.Title}'" + (isOnSale ? " (sale)" : "")
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();  // Để có order.Id

            // 6. Lấy 1 key Available + gán key atomic (chống 2 người mua cùng lúc)
            var availableKeyQuery = context.GameKeys
                .AsNoTracking()
                .Where(k => k.GameId == gameId && k.Status == KeyStatus.Available);

            var key = await availableKeyQuery.FirstOrDefaultAsync();
            if (key is null) return Result<OrderDto>.Failure("Game đã hết key.");

            // Cập nhật atomic
            var rowsAffected = await context.GameKeys
                .Where(k => k.Id == key.Id && k.Status == KeyStatus.Available)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(k => k.Status, KeyStatus.Activated)
                    .SetProperty(k => k.OrderId, order.Id)
                    .SetProperty(k => k.ActivatedAt, now));

            if (rowsAffected == 0)
            {
                return Result<OrderDto>.Failure("Giao dịch xung đột, vui lòng thử lại.");
            }

            // 7. Trừ Balance
            wallet.Balance -= totalAmount;
            wallet.UpdatedAt = now;

            // 9. Tạo OrderItem
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                GameId = gameId,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
            context.OrderItems.Add(orderItem);

            // 10. Tạo Transaction Purchase (Amount ÂM)
            var txn = new Transaction
            {
                UserId = userId,
                WalletId = wallet.Id,
                Amount = -totalAmount,
                Type = TransactionType.Purchase,
                Status = TransactionStatus.Success,
                Description = $"Mua game '{game.Title}' - đơn {orderCode}"
            };
            context.Transactions.Add(txn);

            await context.SaveChangesAsync();  // RowVersion auto-check tại đây

            // Lấy email trước khi commit để tránh dùng context đã disposed
            var userEmail = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            await transaction.CommitAsync();

            // 11. Gửi email xác nhận (fire-and-forget — không block luồng chính)
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var emailBody = $"""
                    <h2>Xác nhận mua game thành công</h2>
                    <p>Mã đơn: <strong>{orderCode}</strong></p>
                    <p>Tên game: <strong>{game.Title}</strong></p>
                    <p>Tổng tiền: <strong>{totalAmount:N0} đ</strong></p>
                    <p>Cảm ơn bạn đã mua game tại Pipe Hub!</p>
                    """;

                _ = _emailService.SendEmailAsync(
                    userEmail,
                    "Xác nhận mua game thành công - Pipe Hub",
                    emailBody);
            }

            // 12. Log (fire-and-forget)
            _ = _logService.WriteAsync(
                action: "PurchaseGame",
                details: $"User {_currentUser.Username} mua '{game.Title}' - {totalAmount:N0} (đơn {orderCode})",
                userId: userId,
                entityName: nameof(Order),
                entityId: order.Id);

            // 13. Load lại để map DTO (cần Items + Keys)
            var freshOrder = await context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Game)
                .Include(o => o.Keys)
                .FirstAsync(o => o.Id == order.Id);

            return Result<OrderDto>.Success(freshOrder.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            // RowVersion conflict — 2 request cùng lúc
            await transaction.RollbackAsync();
            return Result<OrderDto>.Failure(
                "Giao dịch bị xung đột do có thao tác đồng thời. Vui lòng thử lại.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var orders = await context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Game)
            .Include(o => o.Keys)
            .Where(o => o.UserId == _currentUser.UserId.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(string? statusFilter = null)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới xem được tất cả đơn hàng.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Game)
            .Include(o => o.Keys)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<OrderStatus>(statusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<OrderDto> GetByIdAsync(int orderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Game)
            .Include(o => o.Keys)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            throw new AppException($"Không tìm thấy đơn hàng Id = {orderId}.");
        }

        // Player chỉ xem đơn của mình
        if (_currentUser.IsPlayer && order.UserId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền xem đơn hàng này.");
        }

        return order.ToDto();
    }
}
