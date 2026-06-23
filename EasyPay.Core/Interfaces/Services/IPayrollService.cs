using EasyPay.Core.DTOs.Payroll;

namespace EasyPay.Core.Interfaces.Services;

public interface IPayrollService
{
    Task<IEnumerable<PayrollDto>> GetAllAsync();
    Task<IEnumerable<PayrollDto>> GetByEmployeeIdAsync(int employeeId);
    Task<PayrollDto?> GetByIdAsync(int id);
    Task<PayrollDto> GeneratePayrollAsync(GeneratePayrollDto dto);
    Task<IEnumerable<PayStubDto>> GetPayStubsAsync(int employeeId);
    /// <summary>Returns pre-calculated allowance/deduction from employee benefits for UI pre-fill</summary>
    Task<BenefitTotalsDto> GetBenefitTotalsForEmployeeAsync(int employeeId);
}
