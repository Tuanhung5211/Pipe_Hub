using Microsoft.EntityFrameworkCore.Storage;
using Pipe_Hub.DAL.Data;

namespace Pipe_Hub.DAL.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly PipeHubDbContext _context;
    private IDbContextTransaction? _transaction;

    private IOrderRepository? _orders;
    private IWalletRepository? _wallets;
    private IGameKeyRepository? _gameKeys;

    public UnitOfWork(PipeHubDbContext context)
    {
        _context = context;
    }

    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IWalletRepository Wallets => _wallets ??= new WalletRepository(_context);
    public IGameKeyRepository GameKeys => _gameKeys ??= new GameKeyRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
        await _context.DisposeAsync();
    }
}
