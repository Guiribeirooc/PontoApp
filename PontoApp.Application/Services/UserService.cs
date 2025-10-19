using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;
using PontoApp.Infrastructure.Security;

namespace PontoApp.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task EnsureRolesSeedAsync(CancellationToken ct = default)
    {
        if (!await _db.Roles.AnyAsync(ct))
        {
            _db.Roles.AddRange(new Role { Name = "Admin" }, new Role { Name = "Employee" });
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CreateAdminAsync(CreateUserAdminDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.CompanyId == dto.CompanyId && u.Email == email && !u.IsDeleted, ct);
        if (exists) throw new InvalidOperationException("Já existe usuário com este e-mail na empresa.");

        var (hash, salt) = PasswordHasher.HashPassword(dto.Password);

        var user = new AppUser
        {
            CompanyId = dto.CompanyId,
            Name = dto.Name.Trim(),
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var adminRoleId = await _db.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstAsync(ct);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRoleId });
        await _db.SaveChangesAsync(ct);

        return user.Id;
    }
}
