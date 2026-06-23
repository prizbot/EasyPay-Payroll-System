using System.ComponentModel.DataAnnotations;

namespace EasyPay.Core.DTOs.Leave;

public class LeaveRequestDto
{
    public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays => (EndDate - StartDate).Days + 1;
    public string? LeaveType { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateLeaveRequestDto
{
    [Required(ErrorMessage = "Employee ID is required.")]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Leave type is required.")]
    public string LeaveType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Reason { get; set; }
}

public class UpdateLeaveStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}
