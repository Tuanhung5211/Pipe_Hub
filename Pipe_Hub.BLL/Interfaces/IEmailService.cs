namespace Pipe_Hub.BLL.Interfaces;

public interface IEmailService
{
    /// <summary>Gửi email bất đồng bộ. Trả về true nếu gửi thành công (hoặc giả lập thành công).</summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
