namespace PontoApp.Web.Support;

public static class LogoExtensions
{
    public static async Task<byte[]?> ToBytesAsync(this IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return null;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
