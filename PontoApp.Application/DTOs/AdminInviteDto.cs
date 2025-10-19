namespace PontoApp.Application.DTOs
{
    public record AdminInviteDto(
        string Token,
        DateTime ExpiresAtUtc
    );
}
