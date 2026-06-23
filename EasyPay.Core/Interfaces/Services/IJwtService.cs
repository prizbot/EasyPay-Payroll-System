using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(UserAccount user, Employee employee);
    string GenerateRefreshToken();
    int? GetUserIdFromToken(string token);
}
