using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Leave;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(ILeaveService leaveService, ILogger<LeaveController> logger)
    {
        _leaveService = leaveService;
        _logger = logger;
    }

    
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> GetAll()
    {
        var leaves = await _leaveService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(leaves));
    }

    
    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var leaves = await _leaveService.GetByEmployeeIdAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(leaves));
    }

    
    [HttpGet("pending")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> GetPending()
    {
        var leaves = await _leaveService.GetPendingAsync();
        return Ok(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(leaves));
    }

    
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateLeaveRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var leave = await _leaveService.SubmitLeaveAsync(dto);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(leave, "Leave request submitted successfully."));
    }

    
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeaveStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdClaim, out var uid) ? uid : 0;

        var updated = await _leaveService.UpdateStatusAsync(id, dto, userId);
        if (updated == null)
            return NotFound(ApiResponse.Fail($"Leave request with ID {id} not found."));

        return Ok(ApiResponse<LeaveRequestDto>.Ok(updated,
            $"Leave request {dto.Status} successfully."));
    }
}