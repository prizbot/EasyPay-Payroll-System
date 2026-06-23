using EasyPay.Core.DTOs.Auth;

namespace EasyPay.Core.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken);
}
