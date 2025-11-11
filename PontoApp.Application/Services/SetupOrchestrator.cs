using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Infrastructure.EF;
using System;
using System.Threading;

namespace PontoApp.Application.Services;

public class SetupOrchestrator(AppDbContext db, IInviteService invite, IOnboardingService onboarding, IUserService users)
{
    private readonly AppDbContext _db = db;
    private readonly IInviteService _invite = invite;
    private readonly IOnboardingService _onboarding = onboarding;
    private readonly IUserService _users = users;

    /// <summary>
    /// Valida/consome o token, cria empresa e admin numa transação.
    /// </summary>
    public async Task<int> RunAsync(string token, OnboardingCreateDto payload, CancellationToken ct = default)
    {
        var valid = await _invite.ValidateAsync(token, ct);
        if (!valid) throw new InvalidOperationException("Convite inválido/expirado.");

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _users.EnsureRolesSeedAsync(ct);
        var (company, _, _) = await _onboarding.CreateCompanyWithAdminAsync(payload, ct);

        var consumed = await _invite.ConsumeAsync(token, ct);
        if (!consumed) throw new InvalidOperationException("Falha ao consumir convite.");

        await tx.CommitAsync(ct);
        return company.Id;
    }
}
