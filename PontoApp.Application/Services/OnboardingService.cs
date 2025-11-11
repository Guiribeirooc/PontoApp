using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;
using PontoApp.Infrastructure.EF;

public class OnboardingService(AppDbContext db) : IOnboardingService
{
    private readonly AppDbContext _db = db;

    public async Task<(Company company, Employee admin)> CreateCompanyWithAdminAsync(OnboardingCreateDto dto)
    {
        var normName = dto.CompanyName.Trim();
        var normEmail = dto.AdminEmail.Trim().ToLowerInvariant();

        using var tx = await _db.Database.BeginTransactionAsync();

        var company = new Company { Name = normName, CNPJ = dto.CompanyDocument ?? "", Active = true };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        var admin = new Employee
        {
            Nome = dto.AdminName.Trim(),
            Email = normEmail,
            Pin = await GenerateNextPinAsync(company.Id),
            IsAdmin = true,
            Ativo = true,
            CompanyId = company.Id
        };
        _db.Employees.Add(admin);
        await _db.SaveChangesAsync();

        CreatePasswordHash(dto.AdminPassword, out var hash, out var salt);
        var user = new AppUser
        {
            CompanyId = company.Id,
            EmployeeId = admin.Id,
            Email = normEmail,
            Name = dto.AdminName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var alreadyLinked = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == Roles.Admin);
        if (!alreadyLinked)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = Roles.Admin
            });
            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();
        return (company, admin);
    }

    private async Task<string> GenerateNextPinAsync(int companyId)
    {
        var lastPinStr = await _db.Employees
            .Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.Pin)
            .Select(e => e.Pin)
            .FirstOrDefaultAsync();

        _ = int.TryParse(lastPinStr, out var last);
        return (last + 1).ToString("D6");
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        hash = pbkdf2.GetBytes(32);
    }
}
