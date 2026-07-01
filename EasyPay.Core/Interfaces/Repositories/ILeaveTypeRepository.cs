using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface ILeaveTypeRepository
{
    Task<IEnumerable<LeaveType>> GetAllAsync();
    Task<IEnumerable<LeaveType>> GetActiveAsync();
    Task<LeaveType?> GetByIdAsync(int id);
    Task<LeaveType> AddAsync(LeaveType leaveType);
    Task<LeaveType> UpdateAsync(LeaveType leaveType);
    Task<bool> ExistsAsync(int id);
    Task<bool> IsActiveAsync(int id);
}