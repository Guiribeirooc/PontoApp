namespace PontoApp.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    public string Email { get; set; } = null!;
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Active { get; set; } = true;

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string? ResetCode { get; set; }
    public DateTime? ResetCodeExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
}
