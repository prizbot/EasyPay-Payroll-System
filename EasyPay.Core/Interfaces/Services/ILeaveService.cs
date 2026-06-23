using EasyPay.Core.DTOs.Leave;

namespace EasyPay.Core.Interfaces.Services;

public interface ILeaveService
{
    Task<IEnumerable<LeaveRequestDto>> GetAllAsync();
    Task<IEnumerable<LeaveRequestDto>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<LeaveRequestDto>> GetPendingAsync();
    Task<LeaveRequestDto> SubmitLeaveAsync(CreateLeaveRequestDto dto);
    Task<LeaveRequestDto?> UpdateStatusAsync(int leaveId, UpdateLeaveStatusDto dto, int approvedByUserId);
}
