using System.Security.Claims;

namespace PontoApp.Application.Contracts;

public interface IAuthService
{
    Task<(int? UserId, int CompanyId, string Name, IEnumerable<string> Roles)?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    ClaimsPrincipal BuildPrincipal(int userId, int companyId, string name, IEnumerable<string> roles);
}
