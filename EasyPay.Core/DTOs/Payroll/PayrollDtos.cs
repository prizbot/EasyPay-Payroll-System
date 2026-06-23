using System.ComponentModel.DataAnnotations;

namespace EasyPay.Core.DTOs.Payroll;

public class PayrollDto
{
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int PayMonth { get; set; }
    public int PayYear { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class GeneratePayrollDto
{
    [Required(ErrorMessage = "Employee ID is required.")]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "PayMonth must be between 1 and 12.")]
    public int PayMonth { get; set; }

    [Required]
    [Range(2000, 2100, ErrorMessage = "PayYear must be between 2000 and 2100.")]
    public int PayYear { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Allowance { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Deduction { get; set; }
}

public class PayStubDto
{
    public int PayStubId { get; set; }
    public int PayrollId { get; set; }
    public DateTime GeneratedDate { get; set; }
    public PayrollDto? Payroll { get; set; }
}

/// <summary>Returned by GetBenefitTotalsForEmployee — used to pre-fill allowance/deduction in React</summary>
public class BenefitTotalsDto
{
    public decimal TotalAllowance { get; set; }
    public decimal TotalDeduction { get; set; }
}
