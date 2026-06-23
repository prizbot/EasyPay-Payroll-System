using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Notification;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger              = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>Get all notifications for the logged-in user (max 50)</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();
        var items = await _notificationService.GetForUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(items));
    }

    /// <summary>Get unread notification count for the logged-in user (for bell badge)</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<UnreadCountDto>.Ok(count));
    }

    /// <summary>Mark a specific notification as read</summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse.Ok("Notification marked as read."));
    }

    /// <summary>Mark all notifications as read for the logged-in user</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();
        await _notificationService.MarkAllReadAsync(userId);
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }
}
