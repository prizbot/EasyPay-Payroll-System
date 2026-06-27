using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EasyPay.Core.Common;
using EasyPay.Core.DTOs.Auth;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed."));

        var result = await _authService.LoginAsync(dto);
        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", dto.Username);
            return Unauthorized(ApiResponse.Fail("Invalid username or password."));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Login successful."));
    }

    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Refresh token is required."));

        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        if (result == null)
            return Unauthorized(ApiResponse.Fail("Invalid or expired refresh token."));

        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Token refreshed."));
    }

    
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse.Fail("Validation failed.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid token."));

        await _authService.ChangePasswordAsync(userId, dto);

        return Ok(ApiResponse.Ok("Password changed successfully. Please log in again with your new password."));
    }
}
