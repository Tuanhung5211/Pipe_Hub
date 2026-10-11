using System.Collections.Generic;

namespace Pipe_Hub.DAL.Entities;

public class Achievement : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconPath { get; set; }

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
