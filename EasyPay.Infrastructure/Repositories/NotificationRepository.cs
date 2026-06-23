using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly EasyPayDbContext _context;

    public NotificationRepository(EasyPayDbContext context) { _context = context; }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) =>
        await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(50)
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(int userId) =>
        await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task CreateAsync(int userId, string title, string message)
    {
        _context.Notifications.Add(new Notification
        {
            UserId      = userId,
            Title       = title,
            Message     = message,
            IsRead      = false,
            CreatedDate = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var n = await _context.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);
        if (n == null) return;
        n.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        unread.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();
    }
}
