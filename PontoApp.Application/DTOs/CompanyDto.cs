namespace PontoApp.Application.DTOs;

public record CreateCompanyDto(
    string Name, string CNPJ, string? IE, string? IM,
    string? Logradouro, string? Numero, string? Complemento,
    string? Bairro, string? Cidade, string? UF, string? CEP, string? Pais,
    string? Telefone, string? EmailContato,
    byte[]? Logo
);

public record UpdateCompanyDto(
    int Id,
    string Name, string CNPJ, string? IE, string? IM,
    string? Logradouro, string? Numero, string? Complemento,
    string? Bairro, string? Cidade, string? UF, string? CEP, string? Pais,
    string? Telefone, string? EmailContato,
    byte[]? Logo,
    bool keepExistingLogo, bool Active
);

public record CompanyDto(
    int Id,
    string Name, string CNPJ, string? IE, string? IM,
    string? Logradouro, string? Numero, string? Complemento,
    string? Bairro, string? Cidade, string? UF, string? CEP, string? Pais,
    string? Telefone, string? EmailContato,
    byte[]? Logo, bool Active, DateTime CreatedAt
);
