namespace EasyPay.Core.Entities;

public class Benefit
{
    public int    BenefitId   { get; set; }
    public string BenefitName { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Allowance = adds to NetSalary, Deduction = subtracts from NetSalary</summary>
    public string BenefitType { get; set; } = "Allowance";

    public virtual ICollection<EmployeeBenefit> EmployeeBenefits { get; set; } = new HashSet<EmployeeBenefit>();
}
