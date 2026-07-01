using EasyPay.Core.DTOs.Leave;

namespace EasyPay.Core.Interfaces.Services;

public interface ILeaveTypeService
{
    Task<IEnumerable<LeaveTypeDto>> GetAllAsync();
    Task<IEnumerable<LeaveTypeDto>> GetActiveAsync();
    Task<LeaveTypeDto?> GetByIdAsync(int id);
    Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeDto dto);
    Task<LeaveTypeDto?> UpdateAsync(int id, UpdateLeaveTypeDto dto);
    Task<bool> DeactivateAsync(int id);
}