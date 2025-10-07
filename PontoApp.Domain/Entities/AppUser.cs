using PontoApp.Domain.Common;

namespace PontoApp.Domain.Entities;

public class AppUser : Entity
{
    public string Email { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string? ResetCode { get; set; }
    public DateTimeOffset? ResetCodeExpiresAt { get; set; }
    public bool IsDeleted { get; set; }
}
