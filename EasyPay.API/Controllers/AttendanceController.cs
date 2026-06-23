using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Attendance;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger            = logger;
    }

    /// <summary>Get all attendance records.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor},{Roles.Manager}")]
    public async Task<IActionResult> GetAll()
    {
        var records = await _attendanceService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    /// <summary>Get attendance by employee ID.</summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var records = await _attendanceService.GetByEmployeeIdAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    /// <summary>Get attendance for a specific date (yyyy-MM-dd).</summary>
    [HttpGet("date/{date}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor},{Roles.Manager}")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var records = await _attendanceService.GetByDateAsync(date);
        return Ok(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    /// <summary>Mark attendance for an employee.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> Mark([FromBody] CreateAttendanceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var record = await _attendanceService.MarkAttendanceAsync(dto);
        return Ok(ApiResponse<AttendanceDto>.Ok(record, "Attendance marked successfully."));
    }

    /// <summary>Get monthly attendance summary for an employee.</summary>
    [HttpGet("summary/{employeeId:int}/{year:int}/{month:int}")]
    public async Task<IActionResult> GetSummary(int employeeId, int year, int month)
    {
        var summary = await _attendanceService.GetSummaryAsync(employeeId, month, year);
        return Ok(ApiResponse<AttendanceSummaryDto>.Ok(summary));
    }
}
