using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly EasyPayDbContext _context;

    public AuditRepository(EasyPayDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string actionName)
    {
        var log = new AuditLog
        {
            UserId     = userId,
            ActionName = actionName,
            ActionDate = DateTime.Now
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync(int take = 100)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.ActionDate)
            .Take(take)
            .ToListAsync();
    }
}
