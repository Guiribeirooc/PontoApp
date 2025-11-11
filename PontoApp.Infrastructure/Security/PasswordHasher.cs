using System.Security.Cryptography;

namespace PontoApp.Infrastructure.Security;

public static class PasswordHasher
{
    // Parâmetros PBKDF2 (ajuste conforme necessidade)
    private const int SaltSize = 16;         // 128-bit
    private const int HashSize = 32;         // 256-bit
    private const int Iterations = 100_000;  // segurança moderna

    public static (byte[] Hash, byte[] Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);
        return (hash, salt);
    }

    public static bool Verify(string password, byte[] hash, byte[] salt)
    {
        if (hash == null || salt == null) return false;

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(HashSize);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }
}
