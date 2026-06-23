using EasyPay.Core.DTOs.Attendance;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEmployeeRepository   _employeeRepo;
    private readonly ILeaveRepository      _leaveRepo;

    public AttendanceService(
        IAttendanceRepository attendanceRepo,
        IEmployeeRepository   employeeRepo,
        ILeaveRepository      leaveRepo)
    {
        _attendanceRepo = attendanceRepo;
        _employeeRepo   = employeeRepo;
        _leaveRepo      = leaveRepo;
    }

    public async Task<IEnumerable<AttendanceDto>> GetAllAsync()
    {
        var records = await _attendanceRepo.GetAllAsync();
        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var records = await _attendanceRepo.GetByEmployeeIdAsync(employeeId);
        return records.Select(MapToDto);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByDateAsync(DateTime date)
    {
        var records = await _attendanceRepo.GetByDateAsync(date);
        return records.Select(MapToDto);
    }

    public async Task<AttendanceDto> MarkAttendanceAsync(CreateAttendanceDto dto)
    {
        var employee = await _employeeRepo.GetByIdAsync(dto.EmployeeId)
            ?? throw new KeyNotFoundException($"Employee with ID {dto.EmployeeId} not found.");

        // ── Duplicate check ───────────────────────────────────────
        var alreadyMarked = await _attendanceRepo.ExistsAsync(dto.EmployeeId, dto.AttendanceDate);
        if (alreadyMarked)
            throw new InvalidOperationException(
                $"Attendance for employee {dto.EmployeeId} on {dto.AttendanceDate:yyyy-MM-dd} is already marked.");

        // ── Approved leave block ──────────────────────────────────
        var onApprovedLeave = await _leaveRepo.HasApprovedLeaveOnDateAsync(
            dto.EmployeeId, dto.AttendanceDate);

        if (onApprovedLeave)
            throw new InvalidOperationException(
                "Attendance cannot be marked during an approved leave period.");

        var attendance = new Attendance
        {
            EmployeeId     = dto.EmployeeId,
            AttendanceDate = dto.AttendanceDate.Date,
            Status         = dto.Status
        };

        var created = await _attendanceRepo.AddAsync(attendance);
        created.Employee = employee;
        return MapToDto(created);
    }

    public async Task<AttendanceSummaryDto> GetSummaryAsync(int employeeId, int month, int year)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found.");

        var records = (await _attendanceRepo.GetByEmployeeAndMonthAsync(employeeId, month, year)).ToList();

        return new AttendanceSummaryDto
        {
            EmployeeId   = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            PresentDays  = records.Count(r => r.Status == "Present"),
            AbsentDays   = records.Count(r => r.Status == "Absent"),
            HalfDays     = records.Count(r => r.Status == "Half Day"),
            LeaveDays    = records.Count(r => r.Status == "Leave"),
            TotalDays    = records.Count
        };
    }

    private static AttendanceDto MapToDto(Attendance a) => new()
    {
        AttendanceId   = a.AttendanceId,
        EmployeeId     = a.EmployeeId,
        EmployeeName   = $"{a.Employee.FirstName} {a.Employee.LastName}",
        AttendanceDate = a.AttendanceDate,
        Status         = a.Status
    };
}
