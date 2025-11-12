using System;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;
using PontoApp.Infrastructure.EF;
using System.Linq;

public class OnboardingService(AppDbContext db) : IOnboardingService
{
    private readonly AppDbContext _db = db;

    public async Task<(Company company, Employee admin, AppUser user)> CreateCompanyWithAdminAsync(OnboardingCreateDto dto, CancellationToken ct = default)
    {
        var normName = dto.CompanyName.Trim();
        var normEmail = dto.AdminEmail.Trim().ToLowerInvariant();
        var normalizedCnpj = OnlyDigits(dto.CompanyDocument);
        if (normalizedCnpj.Length != 14)
            throw new ArgumentException("CNPJ inválido para cadastro da empresa.");

        var normalizedCpf = OnlyDigits(dto.AdminCpf);
        if (normalizedCpf.Length != 11)
            throw new ArgumentException("CPF inválido para o administrador.");

        var company = new Company
        {
            Name = normName,
            CNPJ = normalizedCnpj,
            IE = dto.CompanyIE,
            IM = dto.CompanyIM,
            Logradouro = dto.CompanyLogradouro,
            Numero = dto.CompanyNumero,
            Complemento = dto.CompanyComplemento,
            Bairro = dto.CompanyBairro,
            Cidade = dto.CompanyCidade,
            UF = dto.CompanyUF?.ToUpperInvariant(),
            CEP = OnlyDigits(dto.CompanyCEP),
            Pais = dto.CompanyPais,
            Telefone = dto.CompanyTelefone,
            EmailContato = dto.CompanyEmailContato?.Trim().ToLowerInvariant(),
            LogoBytes = dto.CompanyLogo,
            Active = true
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        var admin = new Employee
        {
            Nome = dto.AdminName.Trim(),
            Email = normEmail,
            Pin = await GenerateNextPinAsync(company.Id, ct),
            IsAdmin = true,
            Ativo = true,
            CompanyId = company.Id,
            Cpf = normalizedCpf,
            BirthDate = dto.AdminBirthDate,
            PhotoPath = dto.AdminPhotoPath,
            Phone = dto.AdminPhone,
            Departamento = dto.AdminDepartment,
            Cargo = dto.AdminRole,
            Matricula = dto.AdminMatricula,
            City = dto.CompanyCidade,
            State = dto.CompanyUF
        };
        _db.Employees.Add(admin);
        await _db.SaveChangesAsync(ct);

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
        await _db.SaveChangesAsync(ct);

        var alreadyLinked = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == Roles.Admin, ct);
        if (!alreadyLinked)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = Roles.Admin
            });
            await _db.SaveChangesAsync(ct);
        }

        return (company, admin, user);
    }

    private async Task<string> GenerateNextPinAsync(int companyId, CancellationToken ct)
    {
        var lastPinStr = await _db.Employees
            .Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.Pin)
            .Select(e => e.Pin)
            .FirstOrDefaultAsync(ct);

        _ = int.TryParse(lastPinStr, out var last);
        return (last + 1).ToString("D6");
    }

    private static string OnlyDigits(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return new string(input.Where(char.IsDigit).ToArray());
    }

    private static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        hash = pbkdf2.GetBytes(32);
    }
}
