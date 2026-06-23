namespace EasyPay.Core.Entities;

public class EmployeeBenefit
{
    public int     EmployeeBenefitId { get; set; }
    public int     EmployeeId        { get; set; }
    public int     BenefitId         { get; set; }
    /// <summary>Amount for this benefit assignment (e.g. PF=1800, Bonus=5000)</summary>
    public decimal Amount            { get; set; }

    public virtual Employee Employee { get; set; } = null!;
    public virtual Benefit  Benefit  { get; set; } = null!;
}
