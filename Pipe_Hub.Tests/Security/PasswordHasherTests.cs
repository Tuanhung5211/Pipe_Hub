using Pipe_Hub.BLL.Security;
using Xunit;

namespace Pipe_Hub.Tests.Security;

/// <summary>
/// Unit test cho PasswordHasher (BCrypt wrapper): hash không lưu plaintext,
/// verify đúng/sai, xử lý hash rác và input rỗng.
/// </summary>
public class PasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsValueDifferentFromPlaintext()
    {
        var hash = PasswordHasher.Hash("secret123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("secret123", hash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentSaltedHashes()
    {
        var hash1 = PasswordHasher.Hash("secret123");
        var hash2 = PasswordHasher.Hash("secret123");

        // BCrypt có salt ngẫu nhiên → 2 hash phải khác nhau nhưng đều verify được
        Assert.NotEqual(hash1, hash2);
        Assert.True(PasswordHasher.Verify("secret123", hash1));
        Assert.True(PasswordHasher.Verify("secret123", hash2));
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("correct-horse");

        Assert.True(PasswordHasher.Verify("correct-horse", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("correct-horse");

        Assert.False(PasswordHasher.Verify("wrong-horse", hash));
    }

    [Fact]
    public void Verify_WithInvalidHash_ReturnsFalse()
    {
        // Hash không đúng định dạng BCrypt → không throw, trả false
        Assert.False(PasswordHasher.Verify("secret123", "not-a-bcrypt-hash"));
    }

    [Fact]
    public void Verify_WithNullOrEmptyPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("secret123");

        Assert.False(PasswordHasher.Verify("", hash));
        Assert.False(PasswordHasher.Verify("secret123", ""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hash_WithBlankPassword_ThrowsArgumentException(string password)
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Hash(password));
    }
}
