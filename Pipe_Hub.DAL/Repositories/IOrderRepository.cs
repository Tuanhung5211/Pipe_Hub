using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByCodeAsync(string orderCode);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId);
}
