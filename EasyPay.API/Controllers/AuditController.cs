using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Audit;
using EasyPay.Core.Enums;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger       = logger;
    }

    /// <summary>Get recent audit logs (Admin only).</summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int take = 100)
    {
        var logs = await _auditService.GetLogsAsync(take);
        return Ok(ApiResponse<IEnumerable<AuditLogDto>>.Ok(logs));
    }
}
