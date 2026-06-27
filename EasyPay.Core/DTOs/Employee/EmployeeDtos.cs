using System.ComponentModel.DataAnnotations;

namespace EasyPay.Core.DTOs.Employee;

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }
    public DateTime JoinDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

public class CreateEmployeeDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }

    [Required(ErrorMessage = "Department is required.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation is required.")]
    public string Designation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Basic salary is required.")]
    [Range(1, double.MaxValue, ErrorMessage = "Salary must be greater than zero.")]
    public decimal BasicSalary { get; set; }

    public string? Address { get; set; }

   
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;


    public string? Password { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public string RoleName { get; set; } = "Employee";
}


public class CreateEmployeeResponseDto
{
    public EmployeeDto Employee { get; set; } = null!;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string Message { get; set; } =
        "Employee created. Share the temporary password with the employee — it will not be shown again.";
}

public class UpdateEmployeeDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string Designation { get; set; } = string.Empty;

    [Range(1, double.MaxValue)]
    public decimal BasicSalary { get; set; }

    public string? Address { get; set; }
    public bool IsActive { get; set; }
}
