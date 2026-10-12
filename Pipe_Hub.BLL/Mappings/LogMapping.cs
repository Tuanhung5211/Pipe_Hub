using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Mappings;

/// <summary>
/// Extension method chuyển đổi Entity LogEntry → LogEntryDto.
/// </summary>
public static class LogMapping
{
    public static LogEntryDto ToDto(this LogEntry entry)
    {
        return new LogEntryDto
        {
            Id = entry.Id,
            Action = entry.Action,
            Details = entry.Details,
            UserId = entry.UserId,
            Username = entry.User?.Username,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            CreatedAt = entry.CreatedAt
        };
    }
}
