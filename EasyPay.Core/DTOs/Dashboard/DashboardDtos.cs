namespace EasyPay.Core.DTOs.Dashboard;

// ── Admin / Manager / PayrollProcessor dashboard ─────────────
public class DashboardStatsDto
{
    public int     TotalEmployees        { get; set; }
    public int     ActiveEmployees       { get; set; }
    public int     PresentToday          { get; set; }
    public int     PendingLeaves         { get; set; }
    public decimal TotalPayrollThisMonth { get; set; }
    public List<DepartmentBreakdownDto> DepartmentBreakdown { get; set; } = new();
    public List<RecentActivityDto>      RecentActivities    { get; set; } = new();
}

public class DepartmentBreakdownDto
{
    public string Department { get; set; } = string.Empty;
    public int    Count      { get; set; }
}

public class RecentActivityDto
{
    public string   ActionName { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
}

// ── Employee self-service dashboard ──────────────────────────
public class EmployeeDashboardDto
{
    public decimal                BasicSalary      { get; set; }
    public decimal                LatestNetSalary  { get; set; }
    public string                 LatestPayPeriod  { get; set; } = string.Empty;
    public int                    PendingLeaves    { get; set; }
    public int                    ApprovedLeaves   { get; set; }
    public int                    TotalLeaves      { get; set; }
    public List<MyBenefitDto>     MyBenefits       { get; set; } = new();
    public MyLatestPayStubDto?    LatestPayStub    { get; set; }
}

public class MyBenefitDto
{
    public string  BenefitName { get; set; } = string.Empty;
    public string  BenefitType { get; set; } = string.Empty;
    public decimal Amount      { get; set; }
}

public class MyLatestPayStubDto
{
    public int      PayMonth    { get; set; }
    public int      PayYear     { get; set; }
    public decimal  BasicSalary { get; set; }
    public decimal  Allowance   { get; set; }
    public decimal  Deduction   { get; set; }
    public decimal  NetSalary   { get; set; }
    public DateTime GeneratedDate { get; set; }
}
