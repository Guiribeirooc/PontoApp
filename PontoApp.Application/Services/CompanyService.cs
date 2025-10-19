using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services;

public class CompanyService(AppDbContext db) : ICompanyService
{
    private readonly AppDbContext _db = db;

    public async Task<int> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default)
    {
        // normaliza CNPJ/CEP (somente números)
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
            LogoBytes = dto.LogoBytes,
            Active = true
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        return company.Id;
    }
}
