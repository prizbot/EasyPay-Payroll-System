using System.ComponentModel.DataAnnotations;

namespace EasyPay.Core.DTOs.Attendance;

public class AttendanceDto
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateAttendanceDto
{
    [Required(ErrorMessage = "Employee ID is required.")]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Attendance date is required.")]
    public DateTime AttendanceDate { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}

public class AttendanceSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int HalfDays { get; set; }
    public int LeaveDays { get; set; }
    public int TotalDays { get; set; }
}
