using Pipe_Hub.BLL.Interfaces;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Chatbot FAQ đơn giản (rule-based): so khớp từ khóa trong câu hỏi
/// và trả về câu trả lời có sẵn. Không dùng AI/LLM.
/// </summary>
public class FaqChatService : IChatService
{
    private const string FallbackMessage =
        "Xin lỗi, tôi chưa hiểu ý bạn. Vui lòng liên hệ Admin để được hỗ trợ.";

    public Task<string> GetResponseAsync(string userMessage)
    {
        var message = (userMessage ?? string.Empty).Trim().ToLowerInvariant();

        if (message.Length == 0)
        {
            return Task.FromResult(FallbackMessage);
        }

        string answer;

        if (ContainsAny(message, "nạp tiền", "nạp ví"))
        {
            answer = "Bạn có thể vào menu Người chơi -> Ví của tôi, bấm 'Nạp ví' và chờ Admin duyệt nhé.";
        }
        else if (ContainsAny(message, "mua game", "thanh toán"))
        {
            answer = "Bạn vào Cửa hàng game, chọn game muốn mua và bấm 'Mua ngay'. Số dư sẽ bị trừ tự động.";
        }
        else if (ContainsAny(message, "đổi mật khẩu", "mật khẩu"))
        {
            answer = "Vào menu Hệ thống -> Đổi mật khẩu để thay đổi mật khẩu cá nhân.";
        }
        else if (ContainsAny(message, "doanh thu", "báo cáo"))
        {
            answer = "Admin/Publisher có thể xem biểu đồ doanh thu tại menu Báo cáo -> Dashboard.";
        }
        else if (ContainsAny(message, "chào", "hello") || message == "hi" || message == "hi!")
        {
            answer = "Xin chào! Tôi là trợ lý ảo Pipe Hub. Tôi có thể giúp gì cho bạn?";
        }
        else if (ContainsAny(message, "cảm ơn", "thanks"))
        {
            answer = "Không có gì! Chúc bạn chơi game vui vẻ.";
        }
        else
        {
            answer = FallbackMessage;
        }

        return Task.FromResult(answer);
    }

    private static bool ContainsAny(string message, params string[] keywords)
    {
        return keywords.Any(keyword => message.Contains(keyword, StringComparison.Ordinal));
    }
}
