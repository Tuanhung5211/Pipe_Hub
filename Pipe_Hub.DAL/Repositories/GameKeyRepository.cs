using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Repositories;

public class GameKeyRepository : Repository<GameKey>, IGameKeyRepository
{
    public GameKeyRepository(PipeHubDbContext context) : base(context) { }

    public async Task<GameKey?> GetAvailableKeyForGameAsync(int gameId)
        => await DbSet.Where(k => k.GameId == gameId && k.Status == KeyStatus.Available)
                      .OrderBy(k => k.Id)
                      .FirstOrDefaultAsync();

    public async Task<int> CountAvailableKeysAsync(int gameId)
        => await DbSet.CountAsync(k => k.GameId == gameId && k.Status == KeyStatus.Available);
}
