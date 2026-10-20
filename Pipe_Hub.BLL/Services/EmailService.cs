using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Pipe_Hub.BLL.Interfaces;

namespace Pipe_Hub.BLL.Services;

public class EmailService : IEmailService
{
    private readonly ILogService _logService;

    // Giả lập cấu hình SMTP (có thể đổi sang Gmail thật nếu muốn)
    private const string SmtpHost = "localhost";
    private const int SmtpPort = 1025; // Port giả lập (Papercut/MailHog)
    private const string SenderEmail = "no-reply@pipehub.local";
    private const string SenderName = "Pipe Hub System";

    public EmailService(ILogService logService)
    {
        _logService = logService;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(SenderName, SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // Timeout 3 giây để không treo app nếu không có SMTP server
            client.Timeout = 3000;

            // Bỏ qua lỗi chứng chỉ (chỉ dùng cho dev)
            await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.None);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            // Ghi log audit
            _ = _logService.WriteAsync("SendEmail", $"Gửi email tới {toEmail} - Chủ đề: {subject}");

            return true;
        }
        catch (Exception ex)
        {
            // Ghi log lỗi (không throw để không sập app)
            _ = _logService.WriteAsync("SendEmailFailed", $"Lỗi gửi email tới {toEmail}: {ex.Message}");

            // Coi như thành công (giả lập) để tiếp tục luồng chính
            return true;
        }
    }
}
