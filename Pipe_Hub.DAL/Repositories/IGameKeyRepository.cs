using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Repositories;

public interface IGameKeyRepository : IRepository<GameKey>
{
    Task<GameKey?> GetAvailableKeyForGameAsync(int gameId);
    Task<int> CountAvailableKeysAsync(int gameId);
}
