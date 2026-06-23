using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByUsernameAsync(string username);
    Task<UserAccount?> GetByEmployeeIdAsync(int employeeId);
    Task<UserAccount?> GetByRefreshTokenAsync(string refreshToken);
    Task<UserAccount> AddAsync(UserAccount userAccount);
    Task<UserAccount> UpdateAsync(UserAccount userAccount);
}
