using System;
using System.Collections.Generic;

namespace Pipe_Hub.DAL.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public Wallet? Wallet { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<PlaySession> PlaySessions { get; set; } = new List<PlaySession>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
