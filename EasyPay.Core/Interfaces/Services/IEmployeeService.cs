using EasyPay.Core.DTOs.Employee;

namespace EasyPay.Core.Interfaces.Services;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<CreateEmployeeResponseDto> CreateAsync(CreateEmployeeDto dto);
    Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task<bool> DeactivateAsync(int id);
}
