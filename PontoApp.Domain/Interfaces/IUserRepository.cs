using PontoApp.Domain.Entities;

namespace PontoApp.Domain.Interfaces;

public interface IUserRepository
{
    IQueryable<AppUser> Query();
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    Task AddRoleAsync(AppUser user, string roleName, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
