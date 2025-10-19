using PontoApp.Application.DTOs;

namespace PontoApp.Application.Contracts;

public interface ICompanyService
{
    Task<int> CreateAsync(CreateCompanyDto dto, CancellationToken ct = default);
}
