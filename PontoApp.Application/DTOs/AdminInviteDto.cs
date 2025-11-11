namespace PontoApp.Application.DTOs
{
    public record AdminInviteDto(
        string Token,
        DateTime ExpiresAtUtc,
        string CompanyName,
        string CompanyDocument
    );

    public record AdminInviteDetailsDto(
        string CompanyName,
        string CompanyDocument,
        DateTime ExpiresAtUtc,
        bool IsConsumed
    );
}
