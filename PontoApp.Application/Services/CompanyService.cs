using Microsoft.EntityFrameworkCore;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services;

public class CompanyService(AppDbContext db) : ICompanyService
{
    private readonly AppDbContext _db = db;

    public async Task<int> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default)
    {
        string? OnlyDigits(string? s) => string.IsNullOrWhiteSpace(s) ? s : new string(s.Where(char.IsDigit).ToArray());

        var company = new Company
        {
            Name = dto.Name.Trim(),
            CNPJ = OnlyDigits(dto.CNPJ)!,
            IE = dto.IE,
            IM = dto.IM,
            Logradouro = dto.Logradouro,
            Numero = dto.Numero,
            Complemento = dto.Complemento,
            Bairro = dto.Bairro,
            Cidade = dto.Cidade,
            UF = dto.UF?.ToUpperInvariant(),
            CEP = OnlyDigits(dto.CEP),
            Pais = dto.Pais,
            Telefone = dto.Telefone,
            EmailContato = dto.EmailContato?.Trim().ToLowerInvariant(),
            LogoBytes = dto.Logo,
            Active = true
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        return company.Id;
    }

    public async Task<CompanyDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (c is null) return null;

        return new CompanyDto(
            c.Id,
            c.Name,
            c.CNPJ,
            c.IE,
            c.IM,
            c.Logradouro,
            c.Numero,
            c.Complemento,
            c.Bairro,
            c.Cidade,
            c.UF,
            c.CEP,
            c.Pais,
            c.Telefone,
            c.EmailContato,
            c.LogoBytes,
            c.Active,
            c.CreatedAt
        );
    }

    public async Task UpdateAsync(UpdateCompanyDto dto, CancellationToken ct = default)
    {
        string? OnlyDigits(string? s) => string.IsNullOrWhiteSpace(s) ? s : new string(s.Where(char.IsDigit).ToArray());

        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
        if (c is null)
            throw new InvalidOperationException("Empresa não encontrada.");

        c.Name = dto.Name?.Trim() ?? c.Name;
        c.CNPJ = OnlyDigits(dto.CNPJ) ?? c.CNPJ;
        c.IE = dto.IE;
        c.IM = dto.IM;
        c.Logradouro = dto.Logradouro;
        c.Numero = dto.Numero;
        c.Complemento = dto.Complemento;
        c.Bairro = dto.Bairro;
        c.Cidade = dto.Cidade;
        c.UF = dto.UF?.ToUpperInvariant();
        c.CEP = OnlyDigits(dto.CEP);
        c.Pais = dto.Pais;
        c.Telefone = dto.Telefone;
        c.EmailContato = dto.EmailContato?.Trim().ToLowerInvariant();

        if (dto.Logo is not null && dto.Logo.Length > 0)
            c.LogoBytes = dto.Logo;

        if (dto.Active)
            c.Active = dto.Active;

        await _db.SaveChangesAsync(ct);
    }
}
