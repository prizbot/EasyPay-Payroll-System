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
    /// <summary>Check if any non-Rejected leave overlaps with the given date range</summary>
    Task<bool> HasOverlapAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeLeaveId = null);
    /// <summary>Find approved leave covering a specific date (for attendance block)</summary>
    Task<bool> HasApprovedLeaveOnDateAsync(int employeeId, DateTime date);
}
