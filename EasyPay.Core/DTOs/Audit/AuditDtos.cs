namespace EasyPay.Core.DTOs.Audit;

public class AuditLogDto
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string? ActionName { get; set; }
    public DateTime ActionDate { get; set; }
}
