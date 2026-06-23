namespace EasyPay.Core.Entities;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
}
