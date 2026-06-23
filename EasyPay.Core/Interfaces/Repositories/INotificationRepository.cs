using EasyPay.Core.Entities;

namespace EasyPay.Core.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task CreateAsync(int userId, string title, string message);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllReadAsync(int userId);
}
