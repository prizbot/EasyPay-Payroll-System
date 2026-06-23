namespace EasyPay.Core.Entities;

public class Employee
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.Now;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual UserAccount? UserAccount { get; set; }
    public virtual ICollection<Attendance> Attendances { get; set; } = new HashSet<Attendance>();
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new HashSet<LeaveRequest>();
    public virtual ICollection<Payroll> Payrolls { get; set; } = new HashSet<Payroll>();
    public virtual ICollection<EmployeeBenefit> EmployeeBenefits { get; set; } = new HashSet<EmployeeBenefit>();
}
