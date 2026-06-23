using Microsoft.AspNetCore.Mvc;
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
        _logger      = logger;
    }

    /// <summary>Authenticate and receive JWT token.</summary>
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

    /// <summary>Refresh access token using refresh token.</summary>
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
}
