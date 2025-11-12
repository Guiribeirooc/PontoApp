using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;

namespace PontoApp.Application.Contracts
{
    public interface IOnboardingService
    {
        Task<(Company company, Employee admin, AppUser user)> CreateCompanyWithAdminAsync(OnboardingCreateDto dto, CancellationToken ct = default);
    }
}
