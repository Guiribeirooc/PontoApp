using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;
    public EmployeeRepository(AppDbContext db) => _db = db;

    public Task<Employee?> GetByPinAsync(string pin, CancellationToken ct = default) =>
        _db.Employees
           .AsNoTracking()
           .FirstOrDefaultAsync(e => e.Pin == pin && e.Ativo && !e.IsDeleted, ct);

    public Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Employees
           .AsNoTracking()
           .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Employee?> GetByIdForUpdateAsync(int id, CancellationToken ct = default) =>
        _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public IQueryable<Employee> Query() =>
        _db.Employees.AsNoTracking().Where(e => !e.IsDeleted);

    public IQueryable<Employee> QueryAll() =>
        _db.Employees.AsQueryable(); 

    public Task AddAsync(Employee e, CancellationToken ct = default)
    {
        _db.Employees.Add(e);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Employee e, CancellationToken ct = default)
    {
        _db.Employees.Update(e);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Employees.FindAsync(new object?[] { id }, ct);
        if (e is null) return;

        e.IsDeleted = true;
        e.DeletedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(e.Pin))
            e.Pin = $"{e.Pin}:{DateTime.Now}";

        _db.Employees.Update(e);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
