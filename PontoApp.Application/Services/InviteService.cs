using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;
using PontoApp.Infrastructure.Security;

namespace PontoApp.Application.Services;

public class InviteService : IInviteService
{
    private readonly AppDbContext _db;
    public InviteService(AppDbContext db) => _db = db;

    public async Task<AdminInviteDto> CreateAdminInviteAsync(TimeSpan validity, int maxUses = 1, CancellationToken ct = default)
    {
        var token = TokenGenerator.NewUrlToken();
        var hash = TokenGenerator.Sha256(token);
        var invite = new AdminInvite
        {
            TokenHash = hash,
            ExpiresAtUtc = DateTime.UtcNow.Add(validity),
            MaxUses = Math.Max(1, maxUses),
            UsedCount = 0
        };
        _db.AdminInvites.Add(invite);
        await _db.SaveChangesAsync(ct);

        return new AdminInviteDto(token, invite.ExpiresAtUtc);
    }

    public async Task<bool> ValidateAsync(string token, CancellationToken ct = default)
    {
        var hash = TokenGenerator.Sha256(token);
        var inv = await _db.AdminInvites.FirstOrDefaultAsync(i => i.TokenHash == hash, ct);
        if (inv is null) return false;
        if (inv.UsedAtUtc != null) return false;
        if (inv.ExpiresAtUtc < DateTime.UtcNow) return false;
        if (inv.UsedCount >= inv.MaxUses) return false;
        return true;
    }

    public async Task<bool> ConsumeAsync(string token, CancellationToken ct = default)
    {
        var hash = TokenGenerator.Sha256(token);
        var inv = await _db.AdminInvites.FirstOrDefaultAsync(i => i.TokenHash == hash, ct);
        if (inv is null) return false;
        if (inv.UsedAtUtc != null || inv.ExpiresAtUtc < DateTime.UtcNow || inv.UsedCount >= inv.MaxUses) return false;

        inv.UsedCount += 1;
        inv.UsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
