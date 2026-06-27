using EasyPay.Core.DTOs.Employee;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using System.Security.Cryptography;

namespace EasyPay.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IUserAccountRepository _userRepo;
    private readonly IAuditService _auditService;

    public EmployeeService(
        IEmployeeRepository employeeRepo,
        IUserAccountRepository userRepo,
        IAuditService auditService)
    {
        _employeeRepo = employeeRepo;
        _userRepo = userRepo;
        _auditService = auditService;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = await _employeeRepo.GetAllAsync();
        return employees.Select(MapToDto);
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = await _employeeRepo.GetByIdAsync(id);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<CreateEmployeeResponseDto> CreateAsync(CreateEmployeeDto dto)
    {
        
        var existing = await _employeeRepo.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Gender = dto.Gender,
            DOB = dto.DOB,
            JoinDate = DateTime.Now,
            Department = dto.Department,
            Designation = dto.Designation,
            BasicSalary = dto.BasicSalary,
            Address = dto.Address,
            IsActive = true
        };

        var created = await _employeeRepo.AddAsync(employee);

       
        var tempPassword = string.IsNullOrWhiteSpace(dto.Password)
            ? GenerateTemporaryPassword()
            : dto.Password;

        var userAccount = new UserAccount
        {
            EmployeeId = created.EmployeeId,
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            RoleName = dto.RoleName,
            MustChangePassword = true          
        };
        await _userRepo.AddAsync(userAccount);

        await _auditService.LogAsync(null,
            $"Employee Created: {created.FirstName} {created.LastName}");

        return new CreateEmployeeResponseDto
        {
            Employee = MapToDto(created),
            TemporaryPassword = tempPassword
        };
    }

    public async Task<EmployeeDto?> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _employeeRepo.GetByIdAsync(id);
        if (employee == null) return null;

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Email = dto.Email;
        employee.Phone = dto.Phone;
        employee.Gender = dto.Gender;
        employee.DOB = dto.DOB;
        employee.Department = dto.Department;
        employee.Designation = dto.Designation;
        employee.BasicSalary = dto.BasicSalary;
        employee.Address = dto.Address;
        employee.IsActive = dto.IsActive;

        var updated = await _employeeRepo.UpdateAsync(employee);
        await _auditService.LogAsync(null, $"Employee Updated: {id}");
        return MapToDto(updated);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var result = await _employeeRepo.DeleteAsync(id);
        if (result)
            await _auditService.LogAsync(null, $"Employee Deactivated: {id}");
        return result;
    }

    
    public static string GenerateTemporaryPassword()
    {
        
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 10000;
        return $"EP@{number:D4}";
    }

    private static EmployeeDto MapToDto(Employee e) => new()
    {
        EmployeeId = e.EmployeeId,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Email = e.Email,
        Phone = e.Phone,
        Gender = e.Gender,
        DOB = e.DOB,
        JoinDate = e.JoinDate,
        Department = e.Department,
        Designation = e.Designation,
        BasicSalary = e.BasicSalary,
        Address = e.Address,
        IsActive = e.IsActive
    };
}
