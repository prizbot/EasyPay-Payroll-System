using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Employee;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class EmployeeServiceTests
{
    private Mock<IEmployeeRepository>    _mockEmployeeRepo;
    private Mock<IUserAccountRepository> _mockUserRepo;
    private Mock<IAuditService>          _mockAuditService;
    private IEmployeeService             _employeeService;

    [SetUp]
    public void SetUp()
    {
        _mockEmployeeRepo  = new Mock<IEmployeeRepository>();
        _mockUserRepo      = new Mock<IUserAccountRepository>();
        _mockAuditService  = new Mock<IAuditService>();

        _employeeService = new EmployeeService(
            _mockEmployeeRepo.Object,
            _mockUserRepo.Object,
            _mockAuditService.Object);
    }

    // ── GetAllAsync ───────────────────────────────────────

    [Test]
    public async Task GetAllAsync_WhenEmployeesExist_ReturnsMappedDtos()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new() { EmployeeId = 1, FirstName = "Priya",   LastName = "R", Email = "priya@test.com",  Department = "IT",      Designation = "Developer", BasicSalary = 50000, IsActive = true,  JoinDate = DateTime.Now },
            new() { EmployeeId = 2, FirstName = "Karthik", LastName = "M", Email = "karthik@test.com", Department = "Finance", Designation = "Analyst",   BasicSalary = 60000, IsActive = true,  JoinDate = DateTime.Now }
        };

        _mockEmployeeRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(employees);

        // Act
        var result = (await _employeeService.GetAllAsync()).ToList();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].FirstName, Is.EqualTo("Priya"));
        Assert.That(result[1].Department, Is.EqualTo("Finance"));

        _mockEmployeeRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllAsync_WhenNoEmployees_ReturnsEmptyList()
    {
        _mockEmployeeRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Employee>());

        var result = await _employeeService.GetAllAsync();

        Assert.That(result, Is.Empty);
    }

    // ── GetByIdAsync ──────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_WhenEmployeeExists_ReturnsDto()
    {
        var emp = new Employee { EmployeeId = 1, FirstName = "Divya", LastName = "S", Email = "divya@test.com", Department = "HR", Designation = "Manager", BasicSalary = 70000, IsActive = true, JoinDate = DateTime.Now };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(emp);

        var result = await _employeeService.GetByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.EmployeeId, Is.EqualTo(1));
        Assert.That(result.FullName, Is.EqualTo("Divya S"));
        Assert.That(result.BasicSalary, Is.EqualTo(70000));
    }

    [Test]
    public async Task GetByIdAsync_WhenEmployeeNotFound_ReturnsNull()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Employee?)null);

        var result = await _employeeService.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    // ── CreateAsync ───────────────────────────────────────

    [Test]
    public async Task CreateAsync_WithValidDto_CreatesEmployeeAndUserAccount()
    {
        var dto = new CreateEmployeeDto
        {
            FirstName   = "Arun",
            LastName    = "K",
            Email       = "arun@test.com",
            Department  = "IT",
            Designation = "Developer",
            BasicSalary = 55000,
            Username    = "arunk",
            Password    = "Secret@123",
            RoleName    = "Employee"
        };

        var createdEmployee = new Employee
        {
            EmployeeId  = 10,
            FirstName   = dto.FirstName,
            LastName    = dto.LastName,
            Email       = dto.Email,
            Department  = dto.Department,
            Designation = dto.Designation,
            BasicSalary = dto.BasicSalary,
            IsActive    = true,
            JoinDate    = DateTime.Now
        };

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email))
            .ReturnsAsync((Employee?)null);

        _mockEmployeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>()))
            .ReturnsAsync(createdEmployee);

        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<UserAccount>()))
            .ReturnsAsync(new UserAccount());

        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _employeeService.CreateAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.EmployeeId, Is.EqualTo(10));
        Assert.That(result.Email, Is.EqualTo("arun@test.com"));

        _mockEmployeeRepo.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Once);
        _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<UserAccount>()), Times.Once);
        _mockAuditService.Verify(a => a.LogAsync(null, It.Is<string>(s => s.Contains("Created"))), Times.Once);
    }

    [Test]
    public void CreateAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        var dto = new CreateEmployeeDto
        {
            FirstName = "X", LastName = "Y", Email = "exists@test.com",
            Department = "IT", Designation = "Dev", BasicSalary = 50000,
            Username = "xy", Password = "pass", RoleName = "Employee"
        };

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email))
            .ReturnsAsync(new Employee { Email = dto.Email });

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _employeeService.CreateAsync(dto));
    }

    // ── UpdateAsync ───────────────────────────────────────

    [Test]
    public async Task UpdateAsync_WhenEmployeeExists_ReturnsUpdatedDto()
    {
        var existing = new Employee { EmployeeId = 5, FirstName = "Old", LastName = "Name", Email = "old@test.com", Department = "IT", Designation = "Dev", BasicSalary = 40000, IsActive = true, JoinDate = DateTime.Now };

        var updateDto = new UpdateEmployeeDto
        {
            FirstName = "New", LastName = "Name", Email = "new@test.com",
            Department = "Finance", Designation = "Analyst", BasicSalary = 65000, IsActive = true
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        _mockEmployeeRepo.Setup(r => r.UpdateAsync(It.IsAny<Employee>()))
            .ReturnsAsync((Employee e) => e);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _employeeService.UpdateAsync(5, updateDto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FirstName, Is.EqualTo("New"));
        Assert.That(result.BasicSalary, Is.EqualTo(65000));
        Assert.That(result.Department, Is.EqualTo("Finance"));
    }

    [Test]
    public async Task UpdateAsync_WhenEmployeeNotFound_ReturnsNull()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Employee?)null);

        var result = await _employeeService.UpdateAsync(999, new UpdateEmployeeDto
        {
            FirstName = "X", LastName = "Y", Email = "x@y.com",
            Department = "IT", Designation = "Dev", BasicSalary = 1000, IsActive = true
        });

        Assert.That(result, Is.Null);
    }

    // ── DeactivateAsync ───────────────────────────────────

    [Test]
    public async Task DeactivateAsync_WhenEmployeeExists_ReturnsTrue()
    {
        _mockEmployeeRepo.Setup(r => r.DeleteAsync(3)).ReturnsAsync(true);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _employeeService.DeactivateAsync(3);

        Assert.That(result, Is.True);
        _mockEmployeeRepo.Verify(r => r.DeleteAsync(3), Times.Once);
    }

    [Test]
    public async Task DeactivateAsync_WhenEmployeeNotFound_ReturnsFalse()
    {
        _mockEmployeeRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).ReturnsAsync(false);

        var result = await _employeeService.DeactivateAsync(999);

        Assert.That(result, Is.False);
    }

    // ── TestCase examples ─────────────────────────────────

    [TestCase(1,  50000, true)]
    [TestCase(2,  70000, true)]
    [TestCase(99, 0,     false)]
    public async Task GetByIdAsync_TestCaseVariants(int id, decimal expectedSalary, bool shouldExist)
    {
        if (shouldExist)
        {
            _mockEmployeeRepo.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync(new Employee { EmployeeId = id, FirstName = "T", LastName = "U", Email = $"t{id}@t.com", Department = "IT", Designation = "Dev", BasicSalary = expectedSalary, IsActive = true, JoinDate = DateTime.Now });

            var result = await _employeeService.GetByIdAsync(id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.BasicSalary, Is.EqualTo(expectedSalary));
        }
        else
        {
            _mockEmployeeRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Employee?)null);
            var result = await _employeeService.GetByIdAsync(id);
            Assert.That(result, Is.Null);
        }
    }
}
