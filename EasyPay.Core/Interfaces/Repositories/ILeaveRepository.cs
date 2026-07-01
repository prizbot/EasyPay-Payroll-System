using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface ILeaveRepository
{
    Task<IEnumerable<LeaveRequest>> GetAllAsync();
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingAsync();
    Task<LeaveRequest?> GetByIdAsync(int id);
    Task<LeaveRequest> AddAsync(LeaveRequest leaveRequest);
    Task<LeaveRequest> UpdateAsync(LeaveRequest leaveRequest);
    Task<int> GetPendingCountAsync();
    Task<bool> HasOverlapAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeLeaveId = null);
    Task<bool> HasApprovedLeaveOnDateAsync(int employeeId, DateTime date);
}