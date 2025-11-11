namespace PontoApp.Application.DTOs
{
    public sealed class OnboardingCreateDto
    {
        public string CompanyName { get; init; } = string.Empty;
        public string? CompanyDocument { get; init; }

        public string AdminName { get; init; } = string.Empty;
        public string AdminEmail { get; init; } = string.Empty;
        public string AdminPassword { get; init; } = string.Empty;
    }

}
