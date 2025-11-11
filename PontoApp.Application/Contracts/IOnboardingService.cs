using PontoApp.Application.DTOs;
using PontoApp.Domain.Entities;

namespace PontoApp.Application.Contracts
{
    public interface IOnboardingService
    {
        Task<(Company company, Employee admin)> CreateCompanyWithAdminAsync(OnboardingCreateDto dto);
    }
}
