using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(PipeHubDbContext context) : base(context) { }

    public async Task<Order?> GetByCodeAsync(string orderCode)
        => await DbSet.FirstOrDefaultAsync(o => o.OrderCode == orderCode);

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId)
        => await DbSet.Where(o => o.UserId == userId)
                      .OrderByDescending(o => o.CreatedAt)
                      .ToListAsync();
}
