using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface IBenefitRepository
{
    Task<IEnumerable<Benefit>> GetAllAsync();
    Task<Benefit?> GetByIdAsync(int id);
    Task<Benefit> AddAsync(Benefit benefit);
    Task<IEnumerable<EmployeeBenefit>> GetEmployeeBenefitsAsync(int employeeId);
    Task<EmployeeBenefit> AssignBenefitAsync(EmployeeBenefit employeeBenefit);
    Task<bool> RemoveBenefitAsync(int employeeBenefitId);
    Task<bool> BenefitAssignedAsync(int employeeId, int benefitId);
    /// <summary>Sum of amounts by BenefitType for payroll auto-calculation</summary>
    Task<(decimal TotalAllowance, decimal TotalDeduction)> GetBenefitTotalsAsync(int employeeId);
}
