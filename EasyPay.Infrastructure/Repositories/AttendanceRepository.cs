using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly EasyPayDbContext _context;

    public AttendanceRepository(EasyPayDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Attendance>> GetAllAsync()
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking()
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AttendanceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByDateAsync(DateTime date)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking()
            .Where(a => a.AttendanceDate.Date == date.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(int employeeId, int month, int year)
    {
        return await _context.Attendances
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                     && a.AttendanceDate.Month == month
                     && a.AttendanceDate.Year == year)
            .ToListAsync();
    }

    public async Task<Attendance?> GetByIdAsync(int id)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AttendanceId == id);
    }

    public async Task<Attendance> AddAsync(Attendance attendance)
    {
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();
        return attendance;
    }

    public async Task<bool> ExistsAsync(int employeeId, DateTime date)
    {
        return await _context.Attendances
            .AnyAsync(a => a.EmployeeId == employeeId
                        && a.AttendanceDate.Date == date.Date);
    }

    public async Task<int> GetPresentCountTodayAsync()
    {
        var today = DateTime.Today;
        return await _context.Attendances
            .CountAsync(a => a.AttendanceDate.Date == today && a.Status == "Present");
    }
}
