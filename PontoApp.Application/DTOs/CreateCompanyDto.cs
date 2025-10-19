namespace PontoApp.Application.DTOs
{
    public record CreateCompanyDto(
        string Name,
        string CNPJ,
        string? IE,
        string? IM,
        string? Logradouro,
        string? Numero,
        string? Complemento,
        string? Bairro,
        string? Cidade,
        string? UF,
        string? CEP,
        string? Pais,
        string? Telefone,
        string? EmailContato,
        byte[]? LogoBytes
    );
}
