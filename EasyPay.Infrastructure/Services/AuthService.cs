using EasyPay.Core.DTOs.Auth;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserAccountRepository _userRepo;
    private readonly IJwtService _jwtService;
    private readonly IAuditService _auditService;

    public AuthService(
        IUserAccountRepository userRepo,
        IJwtService jwtService,
        IAuditService auditService)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _auditService = auditService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userRepo.GetByUsernameAsync(dto.Username);

        if (user == null)
            return null;

        bool valid;

        // BCrypt hashed password
        if (!string.IsNullOrWhiteSpace(user.PasswordHash)
            && user.PasswordHash.StartsWith("$2"))
        {
            valid = BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.PasswordHash);
        }
        else
        {
            // Legacy plain-text seeded accounts
            valid = user.PasswordHash == dto.Password;
        }

        if (!valid)
            return null;

        var accessToken = _jwtService.GenerateAccessToken(
            user,
            user.Employee);

        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _userRepo.UpdateAsync(user);

        await _auditService.LogAsync(
            user.UserId,
            $"Login: {user.Username}");

        return BuildResponse(
            user,
            accessToken,
            refreshToken);
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(
        string refreshToken)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(
            refreshToken);

        if (user == null ||
            user.RefreshTokenExpiry < DateTime.UtcNow)
            return null;

        var newAccessToken =
            _jwtService.GenerateAccessToken(
                user,
                user.Employee);

        var newRefreshToken =
            _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry =
            DateTime.UtcNow.AddDays(7);

        await _userRepo.UpdateAsync(user);

        return BuildResponse(
            user,
            newAccessToken,
            newRefreshToken);
    }

    private static LoginResponseDto BuildResponse(
        UserAccount user,
        string access,
        string refresh)
    {
        return new LoginResponseDto
        {
            Token = access,
            RefreshToken = refresh,
            Username = user.Username,
            Role = user.RoleName,
            EmployeeId = user.EmployeeId,
            FullName =
                $"{user.Employee.FirstName} {user.Employee.LastName}",
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }
}