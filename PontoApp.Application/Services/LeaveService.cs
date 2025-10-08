using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;
using PontoApp.Infrastructure.EF;

namespace PontoApp.Application.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly AppDbContext _db;
        public LeaveService(AppDbContext db) => _db = db;

        public async Task<List<Leave>> ListAsync(int? employeeId, DateOnly? from, DateOnly? to, LeaveType? type, CancellationToken ct)
        {
            var q = _db.Leaves.AsQueryable();
            if (employeeId.HasValue) q = q.Where(x => x.EmployeeId == employeeId);
            if (type.HasValue) q = q.Where(x => x.Type == type);
            if (from.HasValue) q = q.Where(x => x.End >= from.Value);
            if (to.HasValue) q = q.Where(x => x.Start <= to.Value);
            return await q.OrderByDescending(x => x.Start).ToListAsync(ct);
        }

        public async Task CreateAsync(Leave leave, CancellationToken ct)
        {
            _db.Leaves.Add(leave);
            await _db.SaveChangesAsync(ct);
        }
    }
}
