namespace PontoApp.Application.DTOs
{
    public record CreateUserAdminDto(
        int CompanyId,
        string Name,
        string Email,
        string Password
    );
}
