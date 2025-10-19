using PontoApp.Application.DTOs;

namespace PontoApp.Application.Contracts;

public interface IUserService
{
    Task<int> CreateAdminAsync(CreateUserAdminDto dto, CancellationToken ct = default);
    Task EnsureRolesSeedAsync(CancellationToken ct = default);
}
