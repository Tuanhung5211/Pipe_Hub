namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho một dòng tin nhắn trong khung chat (Chatbot FAQ).
/// </summary>
public record ChatMessageDto
{
    /// <summary>true: User gửi, false: Bot trả lời.</summary>
    public bool IsUser { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.Now;
}
