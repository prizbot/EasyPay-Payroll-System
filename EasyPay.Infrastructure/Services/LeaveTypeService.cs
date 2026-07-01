using EasyPay.Core.DTOs.Leave;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveTypeRepository _leaveTypeRepo;

    public LeaveTypeService(ILeaveTypeRepository leaveTypeRepo)
    {
        _leaveTypeRepo = leaveTypeRepo;
    }

    public async Task<IEnumerable<LeaveTypeDto>> GetAllAsync()
    {
        var types = await _leaveTypeRepo.GetAllAsync();
        return types.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaveTypeDto>> GetActiveAsync()
    {
        var types = await _leaveTypeRepo.GetActiveAsync();
        return types.Select(MapToDto);
    }

    public async Task<LeaveTypeDto?> GetByIdAsync(int id)
    {
        var lt = await _leaveTypeRepo.GetByIdAsync(id);
        return lt == null ? null : MapToDto(lt);
    }

    public async Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeDto dto)
    {
        var leaveType = new LeaveType
        {
            Name = dto.Name,
            IsPaid = dto.IsPaid,
            AnnualAllowance = dto.AnnualAllowance,
            Description = dto.Description,
            IsActive = dto.IsActive
        };
        var created = await _leaveTypeRepo.AddAsync(leaveType);
        return MapToDto(created);
    }

    public async Task<LeaveTypeDto?> UpdateAsync(int id, UpdateLeaveTypeDto dto)
    {
        var leaveType = await _leaveTypeRepo.GetByIdAsync(id);
        if (leaveType == null) return null;

        leaveType.Name = dto.Name;
        leaveType.IsPaid = dto.IsPaid;
        leaveType.AnnualAllowance = dto.AnnualAllowance;
        leaveType.Description = dto.Description;
        leaveType.IsActive = dto.IsActive;

        var updated = await _leaveTypeRepo.UpdateAsync(leaveType);
        return MapToDto(updated);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var leaveType = await _leaveTypeRepo.GetByIdAsync(id);
        if (leaveType == null) return false;

        leaveType.IsActive = false;
        await _leaveTypeRepo.UpdateAsync(leaveType);
        return true;
    }

    private static LeaveTypeDto MapToDto(LeaveType lt) => new()
    {
        LeaveTypeId = lt.LeaveTypeId,
        Name = lt.Name,
        IsPaid = lt.IsPaid,
        AnnualAllowance = lt.AnnualAllowance,
        Description = lt.Description,
        IsActive = lt.IsActive
    };
}