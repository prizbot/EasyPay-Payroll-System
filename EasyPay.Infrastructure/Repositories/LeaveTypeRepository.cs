using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly EasyPayDbContext _context;

    public LeaveTypeRepository(EasyPayDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveType>> GetAllAsync() =>
        await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(lt => lt.Name)
            .ToListAsync();

    public async Task<IEnumerable<LeaveType>> GetActiveAsync() =>
        await _context.LeaveTypes
            .AsNoTracking()
            .Where(lt => lt.IsActive)
            .OrderBy(lt => lt.Name)
            .ToListAsync();

    public async Task<LeaveType?> GetByIdAsync(int id) =>
        await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(lt => lt.LeaveTypeId == id);

    public async Task<LeaveType> AddAsync(LeaveType leaveType)
    {
        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();
        return leaveType;
    }

    public async Task<LeaveType> UpdateAsync(LeaveType leaveType)
    {
        _context.LeaveTypes.Update(leaveType);
        await _context.SaveChangesAsync();
        return leaveType;
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _context.LeaveTypes.AnyAsync(lt => lt.LeaveTypeId == id);

    public async Task<bool> IsActiveAsync(int id) =>
        await _context.LeaveTypes.AnyAsync(lt => lt.LeaveTypeId == id && lt.IsActive);
}