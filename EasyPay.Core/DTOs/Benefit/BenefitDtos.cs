using System.ComponentModel.DataAnnotations;

namespace EasyPay.Core.DTOs.Benefit;

public class BenefitDto
{
    public int     BenefitId   { get; set; }
    public string  BenefitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Allowance or Deduction</summary>
    public string  BenefitType { get; set; } = "Allowance";
}

public class CreateBenefitDto
{
    [Required(ErrorMessage = "Benefit name is required.")]
    [StringLength(100)]
    public string BenefitName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "BenefitType is required.")]
    [RegularExpression("^(Allowance|Deduction)$",
        ErrorMessage = "BenefitType must be 'Allowance' or 'Deduction'.")]
    public string BenefitType { get; set; } = "Allowance";
}

public class EmployeeBenefitDto
{
    public int     EmployeeBenefitId { get; set; }
    public int     EmployeeId        { get; set; }
    public string  EmployeeName      { get; set; } = string.Empty;
    public int     BenefitId         { get; set; }
    public string  BenefitName       { get; set; } = string.Empty;
    public string? Description       { get; set; }
    public string  BenefitType       { get; set; } = "Allowance";
    public decimal Amount            { get; set; }
}

public class AssignBenefitDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int BenefitId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
}

/// <summary>Used by PayrollService and React to auto-populate allowance/deduction</summary>
public class EmployeeBenefitSummaryDto
{
    public decimal TotalAllowance { get; set; }
    public decimal TotalDeduction { get; set; }
    public List<EmployeeBenefitDto> Benefits { get; set; } = new();
}
