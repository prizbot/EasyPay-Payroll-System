using Microsoft.EntityFrameworkCore;
using EasyPay.Core.Entities;
using EasyPay.Infrastructure.Data;

namespace EasyPay.Tests.Helpers;

public static class TestDbContextFactory
{
    public static EasyPayDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<EasyPayDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new EasyPayDbContext(options);
    }

    public static Employee MakeEmployee(int id = 1, string dept = "IT", decimal salary = 50000m)
    {
        return new Employee
        {
            EmployeeId  = id,
            FirstName   = "Test",
            LastName    = $"User{id}",
            Email       = $"test{id}@easypay.com",
            Department  = dept,
            Designation = "Developer",
            BasicSalary = salary,
            IsActive    = true,
            JoinDate    = DateTime.Now
        };
    }

    public static UserAccount MakeUserAccount(int userId, int empId, string role = "Employee")
    {
        return new UserAccount
        {
            UserId       = userId,
            EmployeeId   = empId,
            Username     = $"user{userId}",
            PasswordHash = "hashed",
            RoleName     = role
        };
    }
}
