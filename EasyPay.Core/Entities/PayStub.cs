namespace EasyPay.Core.Entities;

public class PayStub
{
    public int PayStubId { get; set; }
    public int PayrollId { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    // Navigation
    public virtual Payroll Payroll { get; set; } = null!;
}
