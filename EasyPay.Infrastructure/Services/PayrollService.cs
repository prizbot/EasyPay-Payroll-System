using EasyPay.Core.DTOs.Payroll;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository     _payrollRepo;
    private readonly IEmployeeRepository    _employeeRepo;
    private readonly IBenefitRepository     _benefitRepo;
    private readonly IAuditService          _auditService;
    private readonly INotificationService   _notificationService;
    private readonly IUserAccountRepository _userRepo;

    public PayrollService(
        IPayrollRepository     payrollRepo,
        IEmployeeRepository    employeeRepo,
        IBenefitRepository     benefitRepo,
        IAuditService          auditService,
        INotificationService   notificationService,
        IUserAccountRepository userRepo)
    {
        _payrollRepo         = payrollRepo;
        _employeeRepo        = employeeRepo;
        _benefitRepo         = benefitRepo;
        _auditService        = auditService;
        _notificationService = notificationService;
        _userRepo            = userRepo;
    }

    public async Task<IEnumerable<PayrollDto>> GetAllAsync()
    {
        var payrolls = await _payrollRepo.GetAllAsync();
        return payrolls.Select(MapToDto);
    }

    public async Task<IEnumerable<PayrollDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var payrolls = await _payrollRepo.GetByEmployeeIdAsync(employeeId);
        return payrolls.Select(MapToDto);
    }

    public async Task<PayrollDto?> GetByIdAsync(int id)
    {
        var payroll = await _payrollRepo.GetByIdAsync(id);
        return payroll == null ? null : MapToDto(payroll);
    }

    public async Task<BenefitTotalsDto> GetBenefitTotalsForEmployeeAsync(int employeeId)
    {
        var (allowance, deduction) = await _benefitRepo.GetBenefitTotalsAsync(employeeId);
        return new BenefitTotalsDto
        {
            TotalAllowance = allowance,
            TotalDeduction = deduction
        };
    }

    public async Task<PayrollDto> GeneratePayrollAsync(GeneratePayrollDto dto)
    {
        var employee = await _employeeRepo.GetByIdAsync(dto.EmployeeId)
            ?? throw new KeyNotFoundException($"Employee with ID {dto.EmployeeId} not found.");

        var existing = await _payrollRepo.GetByEmployeeMonthYearAsync(
            dto.EmployeeId, dto.PayMonth, dto.PayYear);
        if (existing != null)
            throw new InvalidOperationException(
                $"Payroll for Employee {dto.EmployeeId} for {dto.PayMonth}/{dto.PayYear} already exists.");

        // ── Auto-calculate from benefits if not overridden ────────
        // If frontend sends 0 for both, pull from benefits automatically.
        // If frontend sends explicit values, use those (admin override).
        decimal allowance = dto.Allowance;
        decimal deduction = dto.Deduction;

        if (allowance == 0 && deduction == 0)
        {
            var (benefitAllowance, benefitDeduction) =
                await _benefitRepo.GetBenefitTotalsAsync(dto.EmployeeId);
            allowance = benefitAllowance;
            deduction = benefitDeduction;
        }

        var netSalary = employee.BasicSalary + allowance - deduction;
        if (netSalary < 0)
            throw new ArgumentException(
                "Net salary cannot be negative. Check allowance and deduction values.");

        var payroll = new Payroll
        {
            EmployeeId  = dto.EmployeeId,
            PayMonth    = dto.PayMonth,
            PayYear     = dto.PayYear,
            BasicSalary = employee.BasicSalary,
            Allowance   = allowance,
            Deduction   = deduction,
            NetSalary   = netSalary,
            PaymentDate = DateTime.Now
        };

        var created = await _payrollRepo.AddAsync(payroll);

        var stub = new PayStub
        {
            PayrollId     = created.PayrollId,
            GeneratedDate = DateTime.Now
        };
        await _payrollRepo.AddPayStubAsync(stub);

        created.Employee = employee;

        await _auditService.LogAsync(null,
            $"Payroll Generated: EmployeeId={dto.EmployeeId}, Month={dto.PayMonth}/{dto.PayYear}");

        // ── Notify employee ───────────────────────────────────────
        var userAccount = await _userRepo.GetByEmployeeIdAsync(dto.EmployeeId);
        if (userAccount != null)
        {
            await _notificationService.NotifyAsync(
                userAccount.UserId,
                "Payroll Generated",
                $"Your salary for {dto.PayMonth}/{dto.PayYear} has been processed. Net salary: ₹{netSalary:N0}.");
        }

        return MapToDto(created);
    }

    public async Task<IEnumerable<PayStubDto>> GetPayStubsAsync(int employeeId)
    {
        var stubs = await _payrollRepo.GetPayStubsByEmployeeIdAsync(employeeId);
        return stubs.Select(s => new PayStubDto
        {
            PayStubId     = s.PayStubId,
            PayrollId     = s.PayrollId,
            GeneratedDate = s.GeneratedDate,
            Payroll       = MapToDto(s.Payroll)
        });
    }

    private static PayrollDto MapToDto(Payroll p) => new()
    {
        PayrollId    = p.PayrollId,
        EmployeeId   = p.EmployeeId,
        EmployeeName = $"{p.Employee.FirstName} {p.Employee.LastName}",
        Department   = p.Employee.Department,
        PayMonth     = p.PayMonth,
        PayYear      = p.PayYear,
        BasicSalary  = p.BasicSalary,
        Allowance    = p.Allowance,
        Deduction    = p.Deduction,
        NetSalary    = p.NetSalary,
        PaymentDate  = p.PaymentDate
    };
}
