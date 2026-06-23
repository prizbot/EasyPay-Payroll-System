using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Payroll;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(IPayrollService payrollService, ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _logger         = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor},{Roles.Manager}")]
    public async Task<IActionResult> GetAll()
    {
        var payrolls = await _payrollService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PayrollDto>>.Ok(payrolls));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payroll = await _payrollService.GetByIdAsync(id);
        if (payroll == null) return NotFound(ApiResponse.Fail($"Payroll record with ID {id} not found."));
        return Ok(ApiResponse<PayrollDto>.Ok(payroll));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var payrolls = await _payrollService.GetByEmployeeIdAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<PayrollDto>>.Ok(payrolls));
    }

    /// <summary>
    /// Returns benefit-calculated allowance/deduction for a given employee.
    /// Called by React when employee is selected in Generate Payroll form.
    /// </summary>
    [HttpGet("benefit-totals/{employeeId:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> GetBenefitTotals(int employeeId)
    {
        var totals = await _payrollService.GetBenefitTotalsForEmployeeAsync(employeeId);
        return Ok(ApiResponse<BenefitTotalsDto>.Ok(totals));
    }

    [HttpPost("generate")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor}")]
    public async Task<IActionResult> Generate([FromBody] GeneratePayrollDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var payroll = await _payrollService.GeneratePayrollAsync(dto);
        return Ok(ApiResponse<PayrollDto>.Ok(payroll, "Payroll generated successfully."));
    }

    [HttpGet("paystubs/{employeeId:int}")]
    public async Task<IActionResult> GetPayStubs(int employeeId)
    {
        var stubs = await _payrollService.GetPayStubsAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<PayStubDto>>.Ok(stubs));
    }
}
