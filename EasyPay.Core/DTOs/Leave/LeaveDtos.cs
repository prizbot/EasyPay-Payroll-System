using System.ComponentModel.DataAnnotations;

namespace EasyPay.Core.DTOs.Leave;


public class LeaveTypeDto
{
    public int LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public int AnnualAllowance { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateLeaveTypeDto
{
    [Required(ErrorMessage = "Leave type name is required.")]
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    public bool IsPaid { get; set; } = true;

    [Range(0, 365, ErrorMessage = "Annual allowance must be between 0 and 365.")]
    public int AnnualAllowance { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateLeaveTypeDto
{
    [Required(ErrorMessage = "Leave type name is required.")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    [Range(0, 365)]
    public int AnnualAllowance { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}


public class LeaveRequestDto
{
    public int LeaveId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays => (EndDate - StartDate).Days + 1;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
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
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid leave type.")]
    public int LeaveTypeId { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

public class UpdateLeaveStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}