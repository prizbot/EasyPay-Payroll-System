using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly EasyPayDbContext _context;

    public UserAccountRepository(EasyPayDbContext context)
    {
        _context = context;
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username) =>
        await _context.UserAccounts
            .Include(u => u.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

    public async Task<UserAccount?> GetByUserIdAsync(int userId) =>
        await _context.UserAccounts
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.UserId == userId);

    public async Task<UserAccount?> GetByEmployeeIdAsync(int employeeId) =>
        await _context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

    public async Task<UserAccount?> GetByRefreshTokenAsync(string refreshToken) =>
        await _context.UserAccounts
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task<UserAccount> AddAsync(UserAccount userAccount)
    {
        _context.UserAccounts.Add(userAccount);
        await _context.SaveChangesAsync();
        return userAccount;
    }

    public async Task<UserAccount> UpdateAsync(UserAccount userAccount)
    {
        _context.UserAccounts.Update(userAccount);
        await _context.SaveChangesAsync();
        return userAccount;
    }
}
