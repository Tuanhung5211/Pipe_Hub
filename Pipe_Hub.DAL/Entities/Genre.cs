using System.Collections.Generic;

namespace Pipe_Hub.DAL.Entities;

public class Genre : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
