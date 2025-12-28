using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Infrastructure.EF;
using PontoApp.Infrastructure.Security;

namespace PontoApp.Application.Services;

public class AuthService(AppDbContext db) : IAuthService
{
    private readonly AppDbContext _db = db;

    public async Task<(int? UserId, int CompanyId, string Name, int? EmployeeId, IEnumerable<string> Roles)?> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => !u.IsDeleted && u.Active && u.Email == email)
            .Select(u => new
            {
                u.Id,
                u.CompanyId,
                u.Name,
                u.EmployeeId,
                u.PasswordHash,
                u.PasswordSalt
            })
            .FirstOrDefaultAsync(ct);

        if (user is null) return null;
        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt)) return null;

        var roles = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           where ur.UserId == user.Id
                           select r.Name).ToListAsync(ct);

        return (user.Id, user.CompanyId, user.Name, user.EmployeeId, roles);
    }

    public ClaimsPrincipal BuildPrincipal(int userId, int companyId, string name, int? employeeId, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, name),
            new("CompanyId", companyId.ToString())
        };

        if (employeeId.HasValue)
            claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var id = new ClaimsIdentity(claims, "Cookies");
        return new ClaimsPrincipal(id);
    }
}
