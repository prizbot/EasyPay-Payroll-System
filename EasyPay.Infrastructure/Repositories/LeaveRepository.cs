using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class LeaveRepository : ILeaveRepository
{
    private readonly EasyPayDbContext _context;

    public LeaveRepository(EasyPayDbContext context) { _context = context; }

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync() =>
        await _context.LeaveRequests.Include(l => l.Employee)
            .AsNoTracking().OrderByDescending(l => l.StartDate).ToListAsync();

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(int employeeId) =>
        await _context.LeaveRequests.Include(l => l.Employee)
            .AsNoTracking().Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.StartDate).ToListAsync();

    public async Task<IEnumerable<LeaveRequest>> GetPendingAsync() =>
        await _context.LeaveRequests.Include(l => l.Employee)
            .AsNoTracking().Where(l => l.Status == "Pending")
            .OrderBy(l => l.StartDate).ToListAsync();

    public async Task<LeaveRequest?> GetByIdAsync(int id) =>
        await _context.LeaveRequests.Include(l => l.Employee)
            .AsNoTracking().FirstOrDefaultAsync(l => l.LeaveId == id);

    public async Task<LeaveRequest> AddAsync(LeaveRequest leaveRequest)
    {
        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();
        return leaveRequest;
    }

    public async Task<LeaveRequest> UpdateAsync(LeaveRequest leaveRequest)
    {
        _context.LeaveRequests.Update(leaveRequest);
        await _context.SaveChangesAsync();
        return leaveRequest;
    }

    public async Task<int> GetPendingCountAsync() =>
        await _context.LeaveRequests.CountAsync(l => l.Status == "Pending");

    /// <summary>
    /// Returns true if any non-Rejected leave for this employee overlaps
    /// with [startDate, endDate]. Excludes a specific leaveId if re-checking.
    /// Overlap rule: newStart <= existingEnd AND newEnd >= existingStart
    /// </summary>
    public async Task<bool> HasOverlapAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeLeaveId = null)
    {
        var query = _context.LeaveRequests
            .Where(l => l.EmployeeId == employeeId
                     && l.Status != "Rejected"
                     && l.StartDate <= endDate
                     && l.EndDate   >= startDate);

        if (excludeLeaveId.HasValue)
            query = query.Where(l => l.LeaveId != excludeLeaveId.Value);

        return await query.AnyAsync();
    }

    /// <summary>
    /// Returns true if there is an Approved leave for this employee that covers the given date.
    /// </summary>
    public async Task<bool> HasApprovedLeaveOnDateAsync(int employeeId, DateTime date) =>
        await _context.LeaveRequests.AnyAsync(l =>
            l.EmployeeId == employeeId &&
            l.Status     == "Approved" &&
            l.StartDate  <= date.Date  &&
            l.EndDate    >= date.Date);
}
