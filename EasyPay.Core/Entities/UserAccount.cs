namespace EasyPay.Core.Entities;

public class UserAccount
{
    public int UserId { get; set; }
    public int EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    
    public bool MustChangePassword { get; set; } = true;

  
    public virtual Employee Employee { get; set; } = null!;
}
