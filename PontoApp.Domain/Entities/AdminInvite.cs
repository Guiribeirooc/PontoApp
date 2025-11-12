namespace PontoApp.Domain.Entities;

public class AdminInvite
{
    public long Id { get; set; }
    public byte[] TokenHash { get; set; } = null!;  // SHA-256 (64 bytes)
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyDocument { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int MaxUses { get; set; } = 1;
    public int UsedCount { get; set; } = 0;
}