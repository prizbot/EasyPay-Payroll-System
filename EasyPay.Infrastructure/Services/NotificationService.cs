using EasyPay.Core.DTOs.Notification;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;

namespace EasyPay.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<NotificationDto>> GetForUserAsync(int userId)
    {
        var items = await _repo.GetByUserIdAsync(userId);
        return items.Select(n => new NotificationDto
        {
            NotificationId = n.NotificationId,
            UserId         = n.UserId,
            Title          = n.Title,
            Message        = n.Message,
            IsRead         = n.IsRead,
            CreatedDate    = n.CreatedDate
        });
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(int userId)
    {
        var count = await _repo.GetUnreadCountAsync(userId);
        return new UnreadCountDto { Count = count };
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        await _repo.MarkAsReadAsync(notificationId, userId);
    }

    public async Task MarkAllReadAsync(int userId)
    {
        await _repo.MarkAllReadAsync(userId);
    }

    public async Task NotifyAsync(int userId, string title, string message)
    {
        await _repo.CreateAsync(userId, title, message);
    }
}
