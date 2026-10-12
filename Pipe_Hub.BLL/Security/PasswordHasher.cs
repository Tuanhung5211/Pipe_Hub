namespace Pipe_Hub.BLL.Security;

/// <summary>
/// Wrapper cho BCrypt.Net — hash và verify password.
/// KHÔNG lưu plaintext password ở bất kỳ đâu.
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hash password với BCrypt (work factor mặc định 11).
    /// </summary>
    public static string Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new ArgumentException(
                "Password không được để trống.", nameof(plainPassword));
        }

        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    /// <summary>
    /// Verify plainPassword với hash đã lưu. Trả về true nếu khớp.
    /// </summary>
    public static bool Verify(string plainPassword, string hashedPassword)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash không hợp lệ (VD: do dữ liệu cũ) → coi như không khớp
            return false;
        }
    }
}
