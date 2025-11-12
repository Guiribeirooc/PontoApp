namespace PontoApp.Application.DTOs
{
    public sealed class OnboardingCreateDto
    {
        public string CompanyName { get; init; } = string.Empty;
        public string? CompanyDocument { get; init; }
        public string? CompanyIE { get; init; }
        public string? CompanyIM { get; init; }
        public string? CompanyLogradouro { get; init; }
        public string? CompanyNumero { get; init; }
        public string? CompanyComplemento { get; init; }
        public string? CompanyBairro { get; init; }
        public string? CompanyCidade { get; init; }
        public string? CompanyUF { get; init; }
        public string? CompanyCEP { get; init; }
        public string? CompanyPais { get; init; }
        public string? CompanyTelefone { get; init; }
        public string? CompanyEmailContato { get; init; }
        public byte[]? CompanyLogo { get; init; }

        public string AdminName { get; init; } = string.Empty;
        public string AdminCpf { get; init; } = string.Empty;
        public string AdminEmail { get; init; } = string.Empty;
        public DateOnly AdminBirthDate { get; init; }
        public string? AdminPhone { get; init; }
        public string? AdminDepartment { get; init; }
        public string? AdminRole { get; init; }
        public string? AdminMatricula { get; init; }
        public string? AdminPhotoPath { get; init; }
        public string AdminPassword { get; init; } = string.Empty;
    }

}
