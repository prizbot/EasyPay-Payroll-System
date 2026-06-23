namespace EasyPay.Core.Entities;

public class Payroll
{
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    public int PayMonth { get; set; }
    public int PayYear { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime? PaymentDate { get; set; }

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
    public virtual PayStub? PayStub { get; set; }
}
