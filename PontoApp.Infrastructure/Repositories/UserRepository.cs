using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    private readonly AppDbContext _db = db;

    public IQueryable<AppUser> Query() => _db.Users.AsNoTracking();

    public Task<AppUser?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var norm = (email ?? string.Empty).Trim().ToLowerInvariant();

        return _db.Users
            .IgnoreQueryFilters() // << ignora CompanyId == 0
            .FirstOrDefaultAsync(u =>
                !u.IsDeleted &&
                Microsoft.EntityFrameworkCore.EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AS") == norm, ct);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var norm = (email ?? string.Empty).Trim().ToLowerInvariant();
        return _db.Users.AnyAsync(u => !u.IsDeleted && u.Email == norm, ct);
    }

    public Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        user.Email = (user.Email ?? string.Empty).Trim().ToLowerInvariant();
        _db.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AppUser user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return;

        user.IsDeleted = true;
        user.ResetCode = null;
        user.ResetCodeExpiresAt = null;

        _db.Users.Update(user);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
