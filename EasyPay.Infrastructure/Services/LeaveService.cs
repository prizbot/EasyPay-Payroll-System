using EasyPay.Core.DTOs.Leave;
using EasyPay.Core.Entities;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepo;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IUserAccountRepository _userRepo;

    public LeaveService(
        ILeaveRepository leaveRepo,
        ILeaveTypeRepository leaveTypeRepo,
        IEmployeeRepository employeeRepo,
        IAuditService auditService,
        INotificationService notificationService,
        IUserAccountRepository userRepo)
    {
        _leaveRepo = leaveRepo;
        _leaveTypeRepo = leaveTypeRepo;
        _employeeRepo = employeeRepo;
        _auditService = auditService;
        _notificationService = notificationService;
        _userRepo = userRepo;
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetAllAsync()
    {
        var leaves = await _leaveRepo.GetAllAsync();
        return leaves.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var leaves = await _leaveRepo.GetByEmployeeIdAsync(employeeId);
        return leaves.Select(MapToDto);
    }

    public async Task<IEnumerable<LeaveRequestDto>> GetPendingAsync()
    {
        var leaves = await _leaveRepo.GetPendingAsync();
        return leaves.Select(MapToDto);
    }

    public async Task<LeaveRequestDto> SubmitLeaveAsync(CreateLeaveRequestDto dto)
    {
        // 1. End date must not be before start date
        if (dto.EndDate < dto.StartDate)
            throw new ArgumentException("End date cannot be before start date.");

        // 2. Past date validation — cannot request leave for past dates
        if (dto.StartDate.Date < DateTime.Today)
            throw new InvalidOperationException("Leave cannot be requested for past dates.");

        // 3. Employee must exist
        var employee = await _employeeRepo.GetByIdAsync(dto.EmployeeId)
            ?? throw new KeyNotFoundException($"Employee with ID {dto.EmployeeId} not found.");

        // 4. Leave type must exist and be active
        var leaveType = await _leaveTypeRepo.GetByIdAsync(dto.LeaveTypeId);
        if (leaveType == null)
            throw new KeyNotFoundException($"Leave type with ID {dto.LeaveTypeId} not found.");
        if (!leaveType.IsActive)
            throw new InvalidOperationException($"Leave type '{leaveType.Name}' is no longer active and cannot be used.");

        // 5. Overlap check — no overlapping non-rejected leaves
        var hasOverlap = await _leaveRepo.HasOverlapAsync(
            dto.EmployeeId, dto.StartDate.Date, dto.EndDate.Date);
        if (hasOverlap)
            throw new InvalidOperationException(
                "Leave request overlaps with an existing leave request.");

        var leave = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            LeaveTypeId = dto.LeaveTypeId,
            LeaveType = leaveType.Name,   // keep old column in sync
            Reason = dto.Reason,
            Status = LeaveStatus.Pending
        };

        var created = await _leaveRepo.AddAsync(leave);
        created.Employee = employee;
        created.LeaveTypeNav = leaveType;

        await _auditService.LogAsync(null,
            $"Leave Submitted: EmployeeId={dto.EmployeeId}, Type={leaveType.Name}");

        return MapToDto(created);
    }

    public async Task<LeaveRequestDto?> UpdateStatusAsync(
        int leaveId, UpdateLeaveStatusDto dto, int approvedByUserId)
    {
        var validStatuses = new[] { LeaveStatus.Approved, LeaveStatus.Rejected };
        if (!validStatuses.Contains(dto.Status))
            throw new ArgumentException(
                $"Invalid status '{dto.Status}'. Must be Approved or Rejected.");

        var leave = await _leaveRepo.GetByIdAsync(leaveId);
        if (leave == null) return null;

        if (leave.Status != LeaveStatus.Pending)
            throw new InvalidOperationException("Only Pending leave requests can be updated.");

        leave.Status = dto.Status;
        var updated = await _leaveRepo.UpdateAsync(leave);

        await _auditService.LogAsync(approvedByUserId,
            $"Leave {dto.Status}: LeaveId={leaveId}");

        // Notify the employee
        var userAccount = await _userRepo.GetByEmployeeIdAsync(leave.EmployeeId);
        if (userAccount != null)
        {
            var leaveTypeName = leave.LeaveTypeNav?.Name ?? leave.LeaveType ?? "Leave";
            var title = $"Leave {dto.Status}";
            var message = dto.Status == LeaveStatus.Approved
                ? $"Your {leaveTypeName} from {leave.StartDate:MMM dd} to {leave.EndDate:MMM dd} has been approved."
                : $"Your {leaveTypeName} from {leave.StartDate:MMM dd} to {leave.EndDate:MMM dd} has been rejected.";

            await _notificationService.NotifyAsync(userAccount.UserId, title, message);
        }

        return MapToDto(updated);
    }

    // ── Mapper ────────────────────────────────────────────────
    private static LeaveRequestDto MapToDto(LeaveRequest l) => new()
    {
        LeaveId = l.LeaveId,
        EmployeeId = l.EmployeeId,
        EmployeeName = $"{l.Employee.FirstName} {l.Employee.LastName}",
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        LeaveTypeId = l.LeaveTypeId,
        LeaveTypeName = l.LeaveTypeNav?.Name ?? l.LeaveType ?? "Unknown",
        IsPaid = l.LeaveTypeNav?.IsPaid ?? true,
        Reason = l.Reason,
        Status = l.Status
    };
}