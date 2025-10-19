namespace PontoApp.Application.DTOs
{
    public record WorkIntervalDto(
        DateTime In,
        DateTime? Out,
        TimeSpan Duration
    );
}
