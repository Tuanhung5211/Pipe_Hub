using System;
using System.Collections.Generic;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Entities;

public class Game : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public DateTime? SaleStartAt { get; set; }
    public DateTime? SaleEndAt { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Draft;
    public bool IsDeleted { get; set; } = false;

    public int PublisherId { get; set; }
    public User Publisher { get; set; } = null!;

    public ICollection<Genre> Genres { get; set; } = new List<Genre>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Language> Languages { get; set; } = new List<Language>();
    public ICollection<GameBuild> Builds { get; set; } = new List<GameBuild>();
    public ICollection<GameKey> Keys { get; set; } = new List<GameKey>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
