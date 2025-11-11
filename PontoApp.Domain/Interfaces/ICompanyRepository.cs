using PontoApp.Domain.Entities;

namespace PontoApp.Domain.Interfaces
{
    public interface ICompanyRepository
    {
        Task<Company> AddAsync(Company company);
        Task<Company?> GetByIdAsync(int id);
        IQueryable<Company> Query();
        Task<bool> ExistsByNameAsync(string name);
        Task SaveChangesAsync();
    }
}
