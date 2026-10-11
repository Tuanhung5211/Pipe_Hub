using System;

namespace Pipe_Hub.DAL.Entities;

public class UserAchievement : BaseEntity
{
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;
}
