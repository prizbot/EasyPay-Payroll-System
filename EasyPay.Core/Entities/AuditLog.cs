namespace EasyPay.Core.Entities;

public class AuditLog
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string? ActionName { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.Now;
}
