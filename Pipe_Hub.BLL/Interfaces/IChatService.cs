using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service trả lời tự động (rule-based) cho khung chat hỗ trợ người dùng.
/// </summary>
public interface IChatService
{
    /// <summary>Nhận câu hỏi từ user, trả về câu trả lời của Bot.</summary>
    Task<string> GetResponseAsync(string userMessage);
}
