using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface IPayrollRepository
{
    Task<IEnumerable<Payroll>> GetAllAsync();
    Task<IEnumerable<Payroll>> GetByEmployeeIdAsync(int employeeId);
    Task<Payroll?> GetByIdAsync(int id);
    Task<Payroll?> GetByEmployeeMonthYearAsync(int employeeId, int month, int year);
    Task<Payroll> AddAsync(Payroll payroll);
    Task<decimal> GetTotalPayrollForMonthAsync(int month, int year);
    Task<IEnumerable<PayStub>> GetPayStubsByEmployeeIdAsync(int employeeId);
    Task AddPayStubAsync(PayStub payStub);
}
