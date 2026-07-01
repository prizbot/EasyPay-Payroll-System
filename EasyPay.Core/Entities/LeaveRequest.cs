namespace EasyPay.Core.Entities;

public class LeaveRequest
{
    public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    
    public int LeaveTypeId { get; set; }

    
    public string? LeaveType { get; set; }

    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
    public virtual LeaveType LeaveTypeNav { get; set; } = null!;
}