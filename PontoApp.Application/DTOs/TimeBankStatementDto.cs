namespace PontoApp.Application.DTOs;

public record TimeBankStatementDto(
    DateOnly At,
    int Minutes,
    string? Reason,
    string? Source
);