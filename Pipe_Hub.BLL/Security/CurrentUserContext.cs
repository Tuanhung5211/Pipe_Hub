namespace Pipe_Hub.BLL.Security;

/// <summary>
/// Lưu thông tin user đang đăng nhập trong session hiện tại.
/// Đăng ký Singleton trong DI. Set sau khi Login thành công, Clear khi Logout.
/// </summary>
public class CurrentUserContext
{
    public int? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? FullName { get; private set; }
    public string? RoleName { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;

    public bool IsAdmin => string.Equals(RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
    public bool IsPublisher => string.Equals(RoleName, "Publisher", StringComparison.OrdinalIgnoreCase);
    public bool IsPlayer => string.Equals(RoleName, "Player", StringComparison.OrdinalIgnoreCase);

    public void SetUser(int userId, string username, string fullName, string roleName)
    {
        UserId = userId;
        Username = username;
        FullName = fullName;
        RoleName = roleName;
    }

    public void Clear()
    {
        UserId = null;
        Username = null;
        FullName = null;
        RoleName = null;
    }
}
