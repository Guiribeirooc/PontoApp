using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;

namespace PontoApp.Application.Contracts
{
    public interface ILeaveService
    {
        Task<List<Leave>> ListAsync(int? employeeId, DateOnly? from, DateOnly? to, LeaveType? type, CancellationToken ct);
        Task CreateAsync(Leave leave, CancellationToken ct);
    }
}
