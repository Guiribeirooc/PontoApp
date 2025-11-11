using PontoApp.Application.DTOs;

namespace PontoApp.Application.Contracts;

public interface IInviteService
{
    Task<AdminInviteDto> CreateAdminInviteAsync(string companyName, string companyDocument, TimeSpan validity, int maxUses = 1, CancellationToken ct = default);
    Task<bool> ValidateAsync(string token, CancellationToken ct = default);
    Task<bool> ConsumeAsync(string token, CancellationToken ct = default);
    Task<AdminInviteDetailsDto?> GetDetailsAsync(string token, CancellationToken ct = default);
}
