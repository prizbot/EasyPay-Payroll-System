using EasyPay.Core.DTOs.Attendance;

namespace EasyPay.Core.Interfaces.Services;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceDto>> GetAllAsync();
    Task<IEnumerable<AttendanceDto>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<AttendanceDto>> GetByDateAsync(DateTime date);
    Task<AttendanceDto> MarkAttendanceAsync(CreateAttendanceDto dto);
    Task<AttendanceSummaryDto> GetSummaryAsync(int employeeId, int month, int year);
}
