namespace PontoApp.Application.DTOs;

public record WorkIntervalDto(
    DateTimeOffset In,
    DateTimeOffset? Out,
    TimeSpan Duration
);
