using PontoApp.Domain.Entities;
namespace PontoApp.Domain.Interfaces;
public interface IEmployeeRepository
{
    Task<Employee?> GetByPinAsync(string pin, CancellationToken ct = default);
    Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default);
    IQueryable<Employee> Query();
    IQueryable<Employee> QueryAll();
    Task AddAsync(Employee e, CancellationToken ct = default);
    Task UpdateAsync(Employee e, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
