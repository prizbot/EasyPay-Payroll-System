using EasyPay.Core.DTOs.Audit;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepo;

    public AuditService(IAuditRepository auditRepo)
    {
        _auditRepo = auditRepo;
    }

    public async Task LogAsync(int? userId, string actionName)
    {
        await _auditRepo.LogAsync(userId, actionName);
    }

    public async Task<IEnumerable<AuditLogDto>> GetLogsAsync(int take = 100)
    {
        var logs = await _auditRepo.GetAllAsync(take);
        return logs.Select(l => new AuditLogDto
        {
            LogId      = l.LogId,
            UserId     = l.UserId,
            ActionName = l.ActionName,
            ActionDate = l.ActionDate
        });
    }
}
