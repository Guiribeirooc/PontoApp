using PontoApp.Application.DTOs;

public interface ICompanyService
{
    Task<int> CreateAsync(CreateCompanyDto dto, CancellationToken ct);
    Task<CompanyDto?> GetByIdAsync(int id, CancellationToken ct);
    Task UpdateAsync(UpdateCompanyDto dto, CancellationToken ct);
}
