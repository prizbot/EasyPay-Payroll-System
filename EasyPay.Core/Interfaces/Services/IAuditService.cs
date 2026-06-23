using EasyPay.Core.DTOs.Audit;

namespace EasyPay.Core.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string actionName);
    Task<IEnumerable<AuditLogDto>> GetLogsAsync(int take = 100);
}
