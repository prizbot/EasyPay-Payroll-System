using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface IAttendanceRepository
{
    Task<IEnumerable<Attendance>> GetAllAsync();
    Task<IEnumerable<Attendance>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Attendance>> GetByDateAsync(DateTime date);
    Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(int employeeId, int month, int year);
    Task<Attendance?> GetByIdAsync(int id);
    Task<Attendance> AddAsync(Attendance attendance);
    Task<bool> ExistsAsync(int employeeId, DateTime date);
    Task<int> GetPresentCountTodayAsync();
}
