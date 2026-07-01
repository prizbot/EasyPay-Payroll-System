using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;

namespace EasyPay.Infrastructure.Data;

public class EasyPayDbContext : DbContext
{
    public EasyPayDbContext(DbContextOptions<EasyPayDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Payroll> Payrolls { get; set; }
    public DbSet<PayStub> PayStubs { get; set; }
    public DbSet<Benefit> Benefits { get; set; }
    public DbSet<EmployeeBenefit> EmployeeBenefits { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Employee ──────────────────────────────────────────────
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("Employee");
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).HasMaxLength(15);
            e.HasIndex(x => x.Phone).IsUnique();
            e.Property(x => x.Gender).HasMaxLength(10);
            e.Property(x => x.JoinDate).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Department).HasMaxLength(50).IsRequired();
            e.Property(x => x.Designation).HasMaxLength(50).IsRequired();
            e.Property(x => x.BasicSalary).HasColumnType("decimal(12,2)");
            e.Property(x => x.Address).HasMaxLength(200);
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });

        // ── UserAccount ───────────────────────────────────────────
        modelBuilder.Entity<UserAccount>(e =>
        {
            e.ToTable("UserAccount");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.RoleName).HasMaxLength(30).IsRequired();
            e.Property(x => x.RefreshToken).HasMaxLength(500);
            e.Property(x => x.MustChangePassword).HasDefaultValue(true);
            e.HasOne(x => x.Employee)
             .WithOne(x => x.UserAccount)
             .HasForeignKey<UserAccount>(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Attendance ────────────────────────────────────────────
        modelBuilder.Entity<Attendance>(e =>
        {
            e.ToTable("Attendance");
            e.HasKey(x => x.AttendanceId);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Employee)
             .WithMany(x => x.Attendances)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LeaveType ─────────────────────────────────────────────
        modelBuilder.Entity<LeaveType>(e =>
        {
            e.ToTable("LeaveType");
            e.HasKey(x => x.LeaveTypeId);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.IsPaid).HasDefaultValue(true);
            e.Property(x => x.AnnualAllowance).HasDefaultValue(0);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.IsActive).HasDefaultValue(true);
        });

        // ── LeaveRequest ──────────────────────────────────────────
        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.ToTable("LeaveRequest");
            e.HasKey(x => x.LeaveId);
            e.Property(x => x.LeaveType).HasMaxLength(30);  // legacy column
            e.Property(x => x.Reason).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");

            e.HasOne(x => x.Employee)
             .WithMany(x => x.LeaveRequests)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.LeaveTypeNav)
             .WithMany(x => x.LeaveRequests)
             .HasForeignKey(x => x.LeaveTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Payroll ───────────────────────────────────────────────
        modelBuilder.Entity<Payroll>(e =>
        {
            e.ToTable("Payroll");
            e.HasKey(x => x.PayrollId);
            e.Property(x => x.BasicSalary).HasColumnType("decimal(12,2)").IsRequired();
            e.Property(x => x.Allowance).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
            e.Property(x => x.Deduction).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
            e.Property(x => x.NetSalary).HasColumnType("decimal(12,2)").IsRequired();
            e.HasOne(x => x.Employee)
             .WithMany(x => x.Payrolls)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PayStub ───────────────────────────────────────────────
        modelBuilder.Entity<PayStub>(e =>
        {
            e.ToTable("PayStub");
            e.HasKey(x => x.PayStubId);
            e.Property(x => x.GeneratedDate).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Payroll)
             .WithOne(x => x.PayStub)
             .HasForeignKey<PayStub>(x => x.PayrollId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Benefit ───────────────────────────────────────────────
        modelBuilder.Entity<Benefit>(e =>
        {
            e.ToTable("Benefit");
            e.HasKey(x => x.BenefitId);
            e.Property(x => x.BenefitName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.BenefitType).HasMaxLength(20).IsRequired().HasDefaultValue("Allowance");
        });

        // ── EmployeeBenefit ───────────────────────────────────────
        modelBuilder.Entity<EmployeeBenefit>(e =>
        {
            e.ToTable("EmployeeBenefit");
            e.HasKey(x => x.EmployeeBenefitId);
            e.Property(x => x.Amount).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
            e.HasOne(x => x.Employee)
             .WithMany(x => x.EmployeeBenefits)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Benefit)
             .WithMany(x => x.EmployeeBenefits)
             .HasForeignKey(x => x.BenefitId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AuditLog ──────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLog");
            e.HasKey(x => x.LogId);
            e.Property(x => x.ActionName).HasMaxLength(100);
            e.Property(x => x.ActionDate).HasDefaultValueSql("GETDATE()");
        });

        // ── Notification ──────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("Notification");
            e.HasKey(x => x.NotificationId);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.Property(x => x.IsRead).HasDefaultValue(false);
            e.Property(x => x.CreatedDate).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}