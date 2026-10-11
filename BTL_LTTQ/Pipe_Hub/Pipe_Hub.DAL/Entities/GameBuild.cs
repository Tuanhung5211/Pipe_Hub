using System;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Entities;

public class GameBuild : BaseEntity
{
    public string Version { get; set; } = string.Empty;
    public BuildChannel Channel { get; set; } = BuildChannel.Stable;
    public string? PatchNotes { get; set; }
    public string? FilePath { get; set; }
    public DateTime? ReleasedAt { get; set; }

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
}
