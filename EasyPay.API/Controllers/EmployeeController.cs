using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Employee;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.PayrollProcessor},{Roles.Manager}")]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Ok(employees));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null)
            return NotFound(ApiResponse.Fail($"Employee with ID {id} not found."));
        return Ok(ApiResponse<EmployeeDto>.Ok(employee));
    }

    
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var result = await _employeeService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetById),
            new { id = result.Employee.EmployeeId },
            ApiResponse<CreateEmployeeResponseDto>.Ok(result,
                "Employee created. Temporary password shown once — share it with the employee."));
    }

    
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var updated = await _employeeService.UpdateAsync(id, dto);
        if (updated == null)
            return NotFound(ApiResponse.Fail($"Employee with ID {id} not found."));
        return Ok(ApiResponse<EmployeeDto>.Ok(updated, "Employee updated successfully."));
    }

   
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _employeeService.DeactivateAsync(id);
        if (!result)
            return NotFound(ApiResponse.Fail($"Employee with ID {id} not found."));
        return Ok(ApiResponse.Ok("Employee deactivated successfully."));
    }
}
