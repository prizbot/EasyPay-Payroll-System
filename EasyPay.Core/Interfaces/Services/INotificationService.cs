using EasyPay.Core.DTOs.Notification;

namespace EasyPay.Core.Interfaces.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetForUserAsync(int userId);
    Task<UnreadCountDto> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllReadAsync(int userId);
    /// <summary>Internal method called by other services to create notifications</summary>
    Task NotifyAsync(int userId, string title, string message);
}
