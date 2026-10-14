using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Repositories;

public class WalletRepository : Repository<Wallet>, IWalletRepository
{
    public WalletRepository(PipeHubDbContext context) : base(context) { }

    public async Task<Wallet?> GetByUserIdAsync(int userId)
        => await DbSet.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<Wallet?> GetByUserIdWithTransactionsAsync(int userId)
        => await DbSet.Include(w => w.Transactions)
                      .FirstOrDefaultAsync(w => w.UserId == userId);
}
