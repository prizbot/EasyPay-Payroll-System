using EasyPay.Core.DTOs.Benefit;
using EasyPay.Core.Entities;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class BenefitService : IBenefitService
{
    private readonly IBenefitRepository      _benefitRepo;
    private readonly IEmployeeRepository     _employeeRepo;
    private readonly INotificationService    _notificationService;
    private readonly IUserAccountRepository  _userRepo;

    public BenefitService(
        IBenefitRepository     benefitRepo,
        IEmployeeRepository    employeeRepo,
        INotificationService   notificationService,
        IUserAccountRepository userRepo)
    {
        _benefitRepo         = benefitRepo;
        _employeeRepo        = employeeRepo;
        _notificationService = notificationService;
        _userRepo            = userRepo;
    }

    public async Task<IEnumerable<BenefitDto>> GetAllAsync()
    {
        var benefits = await _benefitRepo.GetAllAsync();
        return benefits.Select(MapBenefitToDto);
    }

    public async Task<BenefitDto> CreateAsync(CreateBenefitDto dto)
    {
        var benefit = new Benefit
        {
            BenefitName = dto.BenefitName,
            Description = dto.Description,
            BenefitType = dto.BenefitType
        };
        var created = await _benefitRepo.AddAsync(benefit);
        return MapBenefitToDto(created);
    }

    public async Task<IEnumerable<EmployeeBenefitDto>> GetEmployeeBenefitsAsync(int employeeId)
    {
        var records = await _benefitRepo.GetEmployeeBenefitsAsync(employeeId);
        return records.Select(MapEbToDto);
    }

    public async Task<EmployeeBenefitSummaryDto> GetEmployeeBenefitSummaryAsync(int employeeId)
    {
        var records = (await _benefitRepo.GetEmployeeBenefitsAsync(employeeId)).ToList();
        var (totalAllowance, totalDeduction) = await _benefitRepo.GetBenefitTotalsAsync(employeeId);

        return new EmployeeBenefitSummaryDto
        {
            TotalAllowance = totalAllowance,
            TotalDeduction = totalDeduction,
            Benefits       = records.Select(MapEbToDto).ToList()
        };
    }

    public async Task<EmployeeBenefitDto> AssignBenefitAsync(AssignBenefitDto dto)
    {
        var employee = await _employeeRepo.GetByIdAsync(dto.EmployeeId)
            ?? throw new KeyNotFoundException($"Employee {dto.EmployeeId} not found.");

        var benefit = await _benefitRepo.GetByIdAsync(dto.BenefitId)
            ?? throw new KeyNotFoundException($"Benefit {dto.BenefitId} not found.");

        var alreadyAssigned = await _benefitRepo.BenefitAssignedAsync(dto.EmployeeId, dto.BenefitId);
        if (alreadyAssigned)
            throw new InvalidOperationException("This benefit is already assigned to the employee.");

        var eb = new EmployeeBenefit
        {
            EmployeeId = dto.EmployeeId,
            BenefitId  = dto.BenefitId,
            Amount     = dto.Amount
        };

        var created = await _benefitRepo.AssignBenefitAsync(eb);

        // ── Notification ──────────────────────────────────────────
        var userAccount = await _userRepo.GetByEmployeeIdAsync(dto.EmployeeId);
        if (userAccount != null)
        {
            await _notificationService.NotifyAsync(
                userAccount.UserId,
                "Benefit Assigned",
                $"{benefit.BenefitName} (₹{dto.Amount:N0}) has been assigned to your account.");
        }

        return new EmployeeBenefitDto
        {
            EmployeeBenefitId = created.EmployeeBenefitId,
            EmployeeId        = dto.EmployeeId,
            EmployeeName      = $"{employee.FirstName} {employee.LastName}",
            BenefitId         = dto.BenefitId,
            BenefitName       = benefit.BenefitName,
            Description       = benefit.Description,
            BenefitType       = benefit.BenefitType,
            Amount            = dto.Amount
        };
    }

    public async Task<bool> RemoveBenefitAsync(int employeeBenefitId) =>
        await _benefitRepo.RemoveBenefitAsync(employeeBenefitId);

    // ── Mappers ───────────────────────────────────────────────
    private static BenefitDto MapBenefitToDto(Benefit b) => new()
    {
        BenefitId   = b.BenefitId,
        BenefitName = b.BenefitName,
        Description = b.Description,
        BenefitType = b.BenefitType
    };

    private static EmployeeBenefitDto MapEbToDto(EmployeeBenefit eb) => new()
    {
        EmployeeBenefitId = eb.EmployeeBenefitId,
        EmployeeId        = eb.EmployeeId,
        EmployeeName      = $"{eb.Employee.FirstName} {eb.Employee.LastName}",
        BenefitId         = eb.BenefitId,
        BenefitName       = eb.Benefit.BenefitName,
        Description       = eb.Benefit.Description,
        BenefitType       = eb.Benefit.BenefitType,
        Amount            = eb.Amount
    };
}
