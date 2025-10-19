using System.Security.Cryptography;
using System.Text;

namespace PontoApp.Infrastructure.Security;

public static class TokenGenerator
{
    public static string NewUrlToken(int bytes = 32)
    {
        var raw = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(raw)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static byte[] Sha256(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return SHA256.HashData(bytes);
    }
}
