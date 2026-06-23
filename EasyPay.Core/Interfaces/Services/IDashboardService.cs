using EasyPay.Core.DTOs.Dashboard;

namespace EasyPay.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(int employeeId);
}
