using EasyPay.Core.DTOs.Benefit;

namespace EasyPay.Core.Interfaces.Services;

public interface IBenefitService
{
    Task<IEnumerable<BenefitDto>> GetAllAsync();
    Task<BenefitDto> CreateAsync(CreateBenefitDto dto);
    Task<IEnumerable<EmployeeBenefitDto>> GetEmployeeBenefitsAsync(int employeeId);
    Task<EmployeeBenefitSummaryDto> GetEmployeeBenefitSummaryAsync(int employeeId);
    Task<EmployeeBenefitDto> AssignBenefitAsync(AssignBenefitDto dto);
    Task<bool> RemoveBenefitAsync(int employeeBenefitId);
}
