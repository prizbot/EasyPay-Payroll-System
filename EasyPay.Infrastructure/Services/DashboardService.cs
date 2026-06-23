using Microsoft.EntityFrameworkCore;
using EasyPay.Core.DTOs.Dashboard;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly EasyPayDbContext      _context;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ILeaveRepository      _leaveRepo;
    private readonly IAuditRepository      _auditRepo;
    private readonly IBenefitRepository    _benefitRepo;

    public DashboardService(
        EasyPayDbContext      context,
        IAttendanceRepository attendanceRepo,
        ILeaveRepository      leaveRepo,
        IAuditRepository      auditRepo,
        IBenefitRepository    benefitRepo)
    {
        _context        = context;
        _attendanceRepo = attendanceRepo;
        _leaveRepo      = leaveRepo;
        _auditRepo      = auditRepo;
        _benefitRepo    = benefitRepo;
    }

    // ── Admin / Manager / PayrollProcessor dashboard ─────────
    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var now = DateTime.Now;

        var totalEmployees  = await _context.Employees.CountAsync();
        var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive);
        var presentToday    = await _attendanceRepo.GetPresentCountTodayAsync();
        var pendingLeaves   = await _leaveRepo.GetPendingCountAsync();
        var payrollTotal    = await _context.Payrolls
            .Where(p => p.PayMonth == now.Month && p.PayYear == now.Year)
            .SumAsync(p => (decimal?)p.NetSalary) ?? 0m;

        var deptBreakdown = await _context.Employees
            .Where(e => e.IsActive)
            .GroupBy(e => e.Department)
            .Select(g => new DepartmentBreakdownDto
            {
                Department = g.Key,
                Count      = g.Count()
            })
            .OrderByDescending(d => d.Count)
            .ToListAsync();

        var recentLogs = await _auditRepo.GetAllAsync(10);
        var recentActivities = recentLogs.Select(l => new RecentActivityDto
        {
            ActionName = l.ActionName ?? "Unknown",
            ActionDate = l.ActionDate
        }).ToList();

        return new DashboardStatsDto
        {
            TotalEmployees        = totalEmployees,
            ActiveEmployees       = activeEmployees,
            PresentToday          = presentToday,
            PendingLeaves         = pendingLeaves,
            TotalPayrollThisMonth = payrollTotal,
            DepartmentBreakdown   = deptBreakdown,
            RecentActivities      = recentActivities
        };
    }

    // ── Employee self-service dashboard ──────────────────────
    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new KeyNotFoundException($"Employee {employeeId} not found.");

        // Leaves
        var leaves = await _context.LeaveRequests
            .Where(l => l.EmployeeId == employeeId)
            .ToListAsync();

        var pendingLeaves  = leaves.Count(l => l.Status == "Pending");
        var approvedLeaves = leaves.Count(l => l.Status == "Approved");

        // Latest pay stub
        var latestPayStub = await _context.PayStubs
            .Include(ps => ps.Payroll)
            .Where(ps => ps.Payroll.EmployeeId == employeeId)
            .OrderByDescending(ps => ps.Payroll.PayYear)
            .ThenByDescending(ps => ps.Payroll.PayMonth)
            .FirstOrDefaultAsync();

        MyLatestPayStubDto? stubDto = null;
        string latestPayPeriod      = "—";
        decimal latestNetSalary     = 0;
        if (latestPayStub != null)
        {
            stubDto = new MyLatestPayStubDto
            {
                PayMonth      = latestPayStub.Payroll.PayMonth,
                PayYear       = latestPayStub.Payroll.PayYear,
                BasicSalary   = latestPayStub.Payroll.BasicSalary,
                Allowance     = latestPayStub.Payroll.Allowance,
                Deduction     = latestPayStub.Payroll.Deduction,
                NetSalary     = latestPayStub.Payroll.NetSalary,
                GeneratedDate = latestPayStub.GeneratedDate
            };
            latestPayPeriod  = $"{latestPayStub.Payroll.PayMonth}/{latestPayStub.Payroll.PayYear}";
            latestNetSalary  = latestPayStub.Payroll.NetSalary;
        }

        // Benefits
        var empBenefits = await _benefitRepo.GetEmployeeBenefitsAsync(employeeId);
        var benefitDtos = empBenefits.Select(eb => new MyBenefitDto
        {
            BenefitName = eb.Benefit.BenefitName,
            BenefitType = eb.Benefit.BenefitType,
            Amount      = eb.Amount
        }).ToList();

        return new EmployeeDashboardDto
        {
            BasicSalary     = employee.BasicSalary,
            LatestNetSalary = latestNetSalary,
            LatestPayPeriod = latestPayPeriod,
            PendingLeaves   = pendingLeaves,
            ApprovedLeaves  = approvedLeaves,
            TotalLeaves     = leaves.Count,
            MyBenefits      = benefitDtos,
            LatestPayStub   = stubDto
        };
    }
}
