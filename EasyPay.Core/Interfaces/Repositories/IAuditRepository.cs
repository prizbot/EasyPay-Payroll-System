using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface IAuditRepository
{
    Task LogAsync(int? userId, string actionName);
    Task<IEnumerable<AuditLog>> GetAllAsync(int take = 100);
}
