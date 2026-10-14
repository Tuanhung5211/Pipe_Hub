using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Repositories;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet?> GetByUserIdWithTransactionsAsync(int userId);
}
