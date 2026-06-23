using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Benefit;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BenefitController : ControllerBase
{
    private readonly IBenefitService _benefitService;
    private readonly ILogger<BenefitController> _logger;

    public BenefitController(IBenefitService benefitService, ILogger<BenefitController> logger)
    {
        _benefitService = benefitService;
        _logger         = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var benefits = await _benefitService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<BenefitDto>>.Ok(benefits));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> Create([FromBody] CreateBenefitDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var benefit = await _benefitService.CreateAsync(dto);
        return Ok(ApiResponse<BenefitDto>.Ok(benefit, "Benefit created successfully."));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var benefits = await _benefitService.GetEmployeeBenefitsAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<EmployeeBenefitDto>>.Ok(benefits));
    }

    /// <summary>Returns summed allowance + deduction for payroll pre-fill</summary>
    [HttpGet("employee/{employeeId:int}/summary")]
    public async Task<IActionResult> GetSummary(int employeeId)
    {
        var summary = await _benefitService.GetEmployeeBenefitSummaryAsync(employeeId);
        return Ok(ApiResponse<EmployeeBenefitSummaryDto>.Ok(summary));
    }

    [HttpPost("assign")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> Assign([FromBody] AssignBenefitDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var assigned = await _benefitService.AssignBenefitAsync(dto);
        return Ok(ApiResponse<EmployeeBenefitDto>.Ok(assigned, "Benefit assigned successfully."));
    }

    [HttpDelete("{employeeBenefitId:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> Remove(int employeeBenefitId)
    {
        var result = await _benefitService.RemoveBenefitAsync(employeeBenefitId);
        if (!result)
            return NotFound(ApiResponse.Fail($"Employee benefit with ID {employeeBenefitId} not found."));
        return Ok(ApiResponse.Ok("Benefit removed successfully."));
    }
}
