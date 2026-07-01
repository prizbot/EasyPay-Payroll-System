using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Leave;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class LeaveTypeController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;
    private readonly ILogger<LeaveTypeController> _logger;

    public LeaveTypeController(ILeaveTypeService leaveTypeService, ILogger<LeaveTypeController> logger)
    {
        _leaveTypeService = leaveTypeService;
        _logger = logger;
    }

    
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> GetAll()
    {
        var types = await _leaveTypeService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<LeaveTypeDto>>.Ok(types));
    }

    
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var types = await _leaveTypeService.GetActiveAsync();
        return Ok(ApiResponse<IEnumerable<LeaveTypeDto>>.Ok(types));
    }

    
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lt = await _leaveTypeService.GetByIdAsync(id);
        if (lt == null)
            return NotFound(ApiResponse.Fail($"Leave type with ID {id} not found."));
        return Ok(ApiResponse<LeaveTypeDto>.Ok(lt));
    }

    
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var created = await _leaveTypeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.LeaveTypeId },
            ApiResponse<LeaveTypeDto>.Ok(created, "Leave type created successfully."));
    }

    
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaveTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var updated = await _leaveTypeService.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse.Fail($"Leave type with ID {id} not found."));

        return Ok(ApiResponse<LeaveTypeDto>.Ok(updated, "Leave type updated successfully."));
    }

    
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _leaveTypeService.DeactivateAsync(id);
        if (!result)
            return NotFound(ApiResponse.Fail($"Leave type with ID {id} not found."));

        return Ok(ApiResponse.Ok("Leave type deactivated successfully."));
    }
}