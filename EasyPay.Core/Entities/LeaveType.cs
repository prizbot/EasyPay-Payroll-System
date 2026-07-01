namespace EasyPay.Core.Entities;

public class LeaveType
{
    public int LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public bool IsPaid { get; set; } = true;
    
    public int AnnualAllowance { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new HashSet<LeaveRequest>();
}