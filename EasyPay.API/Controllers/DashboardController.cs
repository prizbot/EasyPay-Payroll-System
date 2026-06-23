using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Dashboard;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger           = logger;
    }

    /// <summary>Admin / Manager / PayrollProcessor stats dashboard</summary>
    [HttpGet("stats")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor},{Roles.Manager}")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }

    /// <summary>Employee self-service dashboard — uses JWT employeeId claim</summary>
    [HttpGet("employee")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<IActionResult> GetEmployeeDashboard()
    {
        var empIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (!int.TryParse(empIdClaim, out var employeeId))
            return Unauthorized(ApiResponse.Fail("Invalid token claims."));

        var data = await _dashboardService.GetEmployeeDashboardAsync(employeeId);
        return Ok(ApiResponse<EmployeeDashboardDto>.Ok(data));
    }
}
