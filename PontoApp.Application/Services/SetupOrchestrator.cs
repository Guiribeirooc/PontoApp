using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services;

public class SetupOrchestrator(AppDbContext db, IInviteService invite, ICompanyService companies, IUserService users)
{
    private readonly AppDbContext _db = db;
    private readonly IInviteService _invite = invite;
    private readonly ICompanyService _companies = companies;
    private readonly IUserService _users = users;

    /// <summary>
    /// Valida/consome o token, cria empresa e admin numa transação.
    /// </summary>
    public async Task<int> RunAsync(string token, CreateCompanyDto company, CreateUserAdminDto adminWithoutCompany, CancellationToken ct = default)
    {
        var valid = await _invite.ValidateAsync(token, ct);
        if (!valid) throw new InvalidOperationException("Convite inválido/expirado.");

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        var companyId = await _companies.CreateAsync(company, ct);
        await _users.EnsureRolesSeedAsync(ct);
        await _users.CreateAdminAsync(adminWithoutCompany with { CompanyId = companyId }, ct);

        var consumed = await _invite.ConsumeAsync(token, ct);
        if (!consumed) throw new InvalidOperationException("Falha ao consumir convite.");

        await tx.CommitAsync(ct);
        return companyId;
    }
}
