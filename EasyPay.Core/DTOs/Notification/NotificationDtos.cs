namespace EasyPay.Core.DTOs.Notification;

public class NotificationDto
{
    public int      NotificationId { get; set; }
    public int      UserId         { get; set; }
    public string   Title          { get; set; } = string.Empty;
    public string   Message        { get; set; } = string.Empty;
    public bool     IsRead         { get; set; }
    public DateTime CreatedDate    { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}
