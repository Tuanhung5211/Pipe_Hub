using System.Collections.Generic;

namespace Pipe_Hub.DAL.Entities;

public class Language : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
