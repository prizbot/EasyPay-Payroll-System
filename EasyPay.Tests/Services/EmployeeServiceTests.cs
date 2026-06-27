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
    private Mock<IEmployeeRepository> _mockEmployeeRepo;
    private Mock<IUserAccountRepository> _mockUserRepo;
    private Mock<IAuditService> _mockAuditService;
    private IEmployeeService _employeeService;

    [SetUp]
    public void SetUp()
    {
        _mockEmployeeRepo = new Mock<IEmployeeRepository>();
        _mockUserRepo = new Mock<IUserAccountRepository>();
        _mockAuditService = new Mock<IAuditService>();

        _employeeService = new EmployeeService(
            _mockEmployeeRepo.Object,
            _mockUserRepo.Object,
            _mockAuditService.Object);
    }

    // ── Temporary password generation ─────────────────────────
    [Test]
    public void GenerateTemporaryPassword_ReturnsExpectedFormat()
    {
        var password = EmployeeService.GenerateTemporaryPassword();

        Assert.That(password, Does.StartWith("EP@"));
        Assert.That(password.Length, Is.EqualTo(7)); // EP@ + 4 digits
        Assert.That(int.TryParse(password.Substring(3), out _), Is.True);
    }

    [Test]
    public void GenerateTemporaryPassword_CalledMultipleTimes_ProducesVariation()
    {
        // Generate 20 passwords and verify at least 2 differ
        // (probability of all 20 being identical is astronomically low)
        var passwords = Enumerable.Range(0, 20)
            .Select(_ => EmployeeService.GenerateTemporaryPassword())
            .ToHashSet();

        Assert.That(passwords.Count, Is.GreaterThan(1));
    }

    [Test]
    public void GenerateTemporaryPassword_NumberPartIsFourDigits()
    {
        // Run 50 times to ensure number is always zero-padded to 4 digits
        for (int i = 0; i < 50; i++)
        {
            var pwd = EmployeeService.GenerateTemporaryPassword();
            var digits = pwd.Substring(3);
            Assert.That(digits.Length, Is.EqualTo(4),
                $"Expected 4-digit suffix but got '{digits}' in password '{pwd}'");
        }
    }

    // ── CreateAsync — stores BCrypt hash ─────────────────────
    [Test]
    public async Task CreateAsync_StoresBCryptHashNotPlainText()
    {
        var dto = MakeCreateDto();

        var createdEmployee = MakeEmployee(10);

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Employee?)null);
        _mockEmployeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>())).ReturnsAsync(createdEmployee);

        UserAccount? capturedAccount = null;
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<UserAccount>()))
            .Callback<UserAccount>(ua => capturedAccount = ua)
            .ReturnsAsync((UserAccount ua) => ua);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _employeeService.CreateAsync(dto);

        Assert.That(capturedAccount, Is.Not.Null);
        // Hash must NOT equal the plain-text password
        Assert.That(capturedAccount!.PasswordHash, Is.Not.EqualTo(dto.Password));
        // Must be a valid BCrypt hash (starts with $2)
        Assert.That(capturedAccount.PasswordHash, Does.StartWith("$2"));
    }

    // ── CreateAsync — MustChangePassword set to true ─────────
    [Test]
    public async Task CreateAsync_SetsMustChangePasswordTrue()
    {
        var dto = MakeCreateDto();

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Employee?)null);
        _mockEmployeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>())).ReturnsAsync(MakeEmployee(11));

        UserAccount? capturedAccount = null;
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<UserAccount>()))
            .Callback<UserAccount>(ua => capturedAccount = ua)
            .ReturnsAsync((UserAccount ua) => ua);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _employeeService.CreateAsync(dto);

        Assert.That(capturedAccount!.MustChangePassword, Is.True);
    }

    // ── CreateAsync — auto-generates temp password when none supplied ──
    [Test]
    public async Task CreateAsync_WhenNoPasswordSupplied_AutoGeneratesTemporaryPassword()
    {
        var dto = MakeCreateDto();
        dto.Password = null;   // no password from admin

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Employee?)null);
        _mockEmployeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>())).ReturnsAsync(MakeEmployee(12));

        UserAccount? capturedAccount = null;
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<UserAccount>()))
            .Callback<UserAccount>(ua => capturedAccount = ua)
            .ReturnsAsync((UserAccount ua) => ua);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _employeeService.CreateAsync(dto);

        // TemporaryPassword in response must follow EP@NNNN format
        Assert.That(result.TemporaryPassword, Does.StartWith("EP@"));
        Assert.That(result.TemporaryPassword.Length, Is.EqualTo(7));
        // Hash must verify against the returned temp password
        Assert.That(BCrypt.Net.BCrypt.Verify(result.TemporaryPassword, capturedAccount!.PasswordHash), Is.True);
    }

    // ── CreateAsync — returns CreateEmployeeResponseDto ───────
    [Test]
    public async Task CreateAsync_ReturnsResponseWithTemporaryPassword()
    {
        var dto = MakeCreateDto();
        dto.Password = "EP@1234";   // explicit password supplied

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Employee?)null);
        _mockEmployeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>())).ReturnsAsync(MakeEmployee(13));
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<UserAccount>())).ReturnsAsync(new UserAccount());
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _employeeService.CreateAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Employee, Is.Not.Null);
        Assert.That(result.TemporaryPassword, Is.EqualTo("EP@1234"));
        Assert.That(result.Message, Is.Not.Empty);
    }

    // ── CreateAsync — duplicate email ─────────────────────────
    [Test]
    public void CreateAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var dto = MakeCreateDto();

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(MakeEmployee(1));

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _employeeService.CreateAsync(dto));
    }

    // ── GetByIdAsync ──────────────────────────────────────────
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeEmployee(1));

        var result = await _employeeService.GetByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.EmployeeId, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

        var result = await _employeeService.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    // ── UpdateAsync ───────────────────────────────────────────
    [Test]
    public async Task UpdateAsync_ExistingEmployee_ReturnsUpdatedDto()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(MakeEmployee(5));
        _mockEmployeeRepo.Setup(r => r.UpdateAsync(It.IsAny<Employee>()))
            .ReturnsAsync((Employee e) => e);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var dto = new UpdateEmployeeDto
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@test.com",
            Department = "HR",
            Designation = "Manager",
            BasicSalary = 65000,
            IsActive = true
        };

        var result = await _employeeService.UpdateAsync(5, dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FirstName, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task UpdateAsync_NonExisting_ReturnsNull()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

        var result = await _employeeService.UpdateAsync(999, new UpdateEmployeeDto
        {
            FirstName = "X",
            LastName = "Y",
            Email = "x@y.com",
            Department = "IT",
            Designation = "Dev",
            BasicSalary = 1000,
            IsActive = true
        });

        Assert.That(result, Is.Null);
    }

    // ── DeactivateAsync ───────────────────────────────────────
    [Test]
    public async Task DeactivateAsync_ExistingEmployee_ReturnsTrue()
    {
        _mockEmployeeRepo.Setup(r => r.DeleteAsync(3)).ReturnsAsync(true);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _employeeService.DeactivateAsync(3);

        Assert.That(result, Is.True);
        _mockEmployeeRepo.Verify(r => r.DeleteAsync(3), Times.Once);
    }

    [Test]
    public async Task DeactivateAsync_NonExisting_ReturnsFalse()
    {
        _mockEmployeeRepo.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _employeeService.DeactivateAsync(999);

        Assert.That(result, Is.False);
    }

    // ── BCrypt hash verification TestCase ─────────────────────
    [TestCase("EP@1234")]
    [TestCase("EP@9999")]
    [TestCase("EP@0001")]
    [TestCase("MyCustomPass@123")]
    public async Task CreateAsync_PasswordHash_VerifiesCorrectlyWithBCrypt(string plainPassword)
    {
        var dto = MakeCreateDto();
        dto.Password = plainPassword;

        _mockEmployeeRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Employee?)null);
        _mockEmployeeRepo.Setup(r => r.AddAsync(It.IsAny<Employee>())).ReturnsAsync(MakeEmployee(20));

        UserAccount? capturedAccount = null;
        _mockUserRepo.Setup(r => r.AddAsync(It.IsAny<UserAccount>()))
            .Callback<UserAccount>(ua => capturedAccount = ua)
            .ReturnsAsync((UserAccount ua) => ua);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _employeeService.CreateAsync(dto);

        Assert.That(
            BCrypt.Net.BCrypt.Verify(plainPassword, capturedAccount!.PasswordHash),
            Is.True,
            $"BCrypt verification failed for password: {plainPassword}");
    }

    // ── Helpers ───────────────────────────────────────────────
    private static CreateEmployeeDto MakeCreateDto() => new()
    {
        FirstName = "Test",
        LastName = "Employee",
        Email = "test@easypay.com",
        Department = "IT",
        Designation = "Developer",
        BasicSalary = 50000,
        Username = "testuser",
        Password = "EP@1234",
        RoleName = "Employee"
    };

    private static Employee MakeEmployee(int id) => new()
    {
        EmployeeId = id,
        FirstName = "Test",
        LastName = "Employee",
        Email = $"test{id}@easypay.com",
        Department = "IT",
        Designation = "Developer",
        BasicSalary = 50000,
        IsActive = true,
        JoinDate = DateTime.Now
    };
}
