namespace Pipe_Hub.DAL.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IOrderRepository Orders { get; }
    IWalletRepository Wallets { get; }
    IGameKeyRepository GameKeys { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
