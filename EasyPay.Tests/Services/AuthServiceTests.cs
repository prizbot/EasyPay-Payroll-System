using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Auth;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserAccountRepository> _mockUserRepo;
    private Mock<IJwtService> _mockJwtService;
    private Mock<IAuditService> _mockAuditService;
    private IAuthService _authService;

    private Employee _testEmployee = null!;
    private UserAccount _testUserAccount = null!;

    [SetUp]
    public void SetUp()
    {
        _mockUserRepo = new Mock<IUserAccountRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockAuditService = new Mock<IAuditService>();

        _authService = new AuthService(
            _mockUserRepo.Object,
            _mockJwtService.Object,
            _mockAuditService.Object);

        _testEmployee = new Employee
        {
            EmployeeId = 1,
            FirstName = "Admin",
            LastName = "User",
            Department = "IT",
            Designation = "Admin",
            BasicSalary = 80000,
            IsActive = true,
            JoinDate = DateTime.Now,
            Email = "admin@easypay.com"
        };

        _testUserAccount = new UserAccount
        {
            UserId = 1,
            EmployeeId = 1,
            Username = "admin",
            PasswordHash = "admin123",   // plain-text fallback path
            RoleName = "Admin",
            MustChangePassword = false,
            Employee = _testEmployee
        };
    }

    // ── LoginAsync — Happy path ───────────────────────────────
    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(_testUserAccount);
        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>())).Returns("access-token");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync(_testUserAccount);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = "admin", Password = "admin123" });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.EqualTo("access-token"));
        Assert.That(result.Role, Is.EqualTo("Admin"));
    }

    // ── LoginAsync — returns MustChangePassword flag ──────────
    [Test]
    public async Task LoginAsync_WhenMustChangePasswordTrue_ReturnsFlagTrue()
    {
        _testUserAccount.MustChangePassword = true;

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(_testUserAccount);
        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>())).Returns("tok");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("ref");
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync(_testUserAccount);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = "admin", Password = "admin123" });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MustChangePassword, Is.True);
    }

    [Test]
    public async Task LoginAsync_WhenMustChangePasswordFalse_ReturnsFlagFalse()
    {
        _testUserAccount.MustChangePassword = false;

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(_testUserAccount);
        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>())).Returns("tok");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("ref");
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync(_testUserAccount);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = "admin", Password = "admin123" });

        Assert.That(result!.MustChangePassword, Is.False);
    }

    // ── LoginAsync — wrong credentials ───────────────────────
    [Test]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(_testUserAccount);

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = "admin", Password = "wrongpass" });

        Assert.That(result, Is.Null);
        _mockJwtService.Verify(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_UnknownUsername_ReturnsNull()
    {
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("nobody")).ReturnsAsync((UserAccount?)null);

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = "nobody", Password = "any" });

        Assert.That(result, Is.Null);
    }

    // ── ChangePasswordAsync — success ─────────────────────────
    [Test]
    public async Task ChangePasswordAsync_ValidCurrentPassword_UpdatesHashAndClearsFlag()
    {
        var currentPlain = "OldPass@123";

        var user = new UserAccount
        {
            UserId = 1,
            EmployeeId = 1,
            Username = "emp1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(currentPlain),
            RoleName = "Employee",
            MustChangePassword = true,
            Employee = _testEmployee
        };

        UserAccount? savedUser = null;

        _mockUserRepo.Setup(r => r.GetByUserIdAsync(1))
            .ReturnsAsync(user);

        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>()))
            .Callback<UserAccount>(u => savedUser = u)
            .ReturnsAsync((UserAccount u) => u);

        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = currentPlain,
            NewPassword = "NewSecure@999"
        };

        await _authService.ChangePasswordAsync(1, changePasswordDto);

        Assert.That(savedUser, Is.Not.Null);
        Assert.That(savedUser!.MustChangePassword, Is.False);

        Assert.That(
            BCrypt.Net.BCrypt.Verify("NewSecure@999", savedUser.PasswordHash),
            Is.True);

        _mockAuditService.Verify(
            a => a.LogAsync(1, It.Is<string>(s => s.Contains("Password Changed"))),
            Times.Once);
    }

    // ── ChangePasswordAsync — wrong current password ──────────
    [Test]
    public void ChangePasswordAsync_WrongCurrentPassword_ThrowsArgumentException()
    {
        var user = new UserAccount
        {
            UserId = 2,
            EmployeeId = 1,
            Username = "emp2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@Pass1"),
            RoleName = "Employee",
            Employee = _testEmployee
        };

        _mockUserRepo.Setup(r => r.GetByUserIdAsync(2)).ReturnsAsync(user);

        var dto = new ChangePasswordDto { CurrentPassword = "WrongPass!", NewPassword = "NewPass@123" };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _authService.ChangePasswordAsync(2, dto));

        Assert.That(ex!.Message, Is.EqualTo("Current password is incorrect."));
        _mockUserRepo.Verify(r => r.UpdateAsync(It.IsAny<UserAccount>()), Times.Never);
    }

    // ── ChangePasswordAsync — same password rejected ──────────
    [Test]
    public void ChangePasswordAsync_NewPasswordSameAsCurrent_ThrowsArgumentException()
    {
        var samePassword = "Same@Pass123";
        var user = new UserAccount
        {
            UserId = 3,
            EmployeeId = 1,
            Username = "emp3",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(samePassword),
            RoleName = "Employee",
            Employee = _testEmployee
        };

        _mockUserRepo.Setup(r => r.GetByUserIdAsync(3)).ReturnsAsync(user);

        var dto = new ChangePasswordDto { CurrentPassword = samePassword, NewPassword = samePassword };

        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _authService.ChangePasswordAsync(3, dto));

        Assert.That(ex!.Message, Does.Contain("different"));
    }

    // ── ChangePasswordAsync — user not found ──────────────────
    [Test]
    public void ChangePasswordAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        _mockUserRepo.Setup(r => r.GetByUserIdAsync(999)).ReturnsAsync((UserAccount?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _authService.ChangePasswordAsync(999,
                new ChangePasswordDto { CurrentPassword = "a", NewPassword = "NewPass@123" }));
    }

    // ── ChangePasswordAsync — sets MustChangePassword false ──
    [Test]
    public async Task ChangePasswordAsync_Success_SetsMustChangePasswordFalse()
    {
        var currentPlain = "Temp@1234";
        var user = new UserAccount
        {
            UserId = 5,
            EmployeeId = 1,
            Username = "emp5",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(currentPlain),
            RoleName = "Employee",
            MustChangePassword = true,
            Employee = _testEmployee
        };

        _mockUserRepo.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync((UserAccount u) => u);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await _authService.ChangePasswordAsync(5,
            new ChangePasswordDto { CurrentPassword = currentPlain, NewPassword = "NewSecure@2026" });

        Assert.That(user.MustChangePassword, Is.False);
    }

    // ── Credential TestCase ───────────────────────────────────
    [TestCase("admin", "admin123", true)]
    [TestCase("admin", "wrongpass", false)]
    [TestCase("nobody", "anypass", false)]
    public async Task LoginAsync_VariousCredentials_ReturnsExpectedResult(
        string username, string password, bool expectSuccess)
    {
        if (username == "admin")
            _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(_testUserAccount);
        else
            _mockUserRepo.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync((UserAccount?)null);

        if (expectSuccess)
        {
            _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>())).Returns("t");
            _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("r");
            _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync(_testUserAccount);
            _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = username, Password = password });

        Assert.That(result != null, Is.EqualTo(expectSuccess));
    }

    // ── RefreshTokenAsync ─────────────────────────────────────
    [Test]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        var userWithToken = new UserAccount
        {
            UserId = 1,
            EmployeeId = 1,
            Username = "admin",
            PasswordHash = "hashed",
            RoleName = "Admin",
            RefreshToken = "valid-refresh",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(5),
            MustChangePassword = false,
            Employee = _testEmployee
        };

        _mockUserRepo.Setup(r => r.GetByRefreshTokenAsync("valid-refresh")).ReturnsAsync(userWithToken);
        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>())).Returns("new-access");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh");
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync(userWithToken);

        var result = await _authService.RefreshTokenAsync("valid-refresh");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.EqualTo("new-access"));
    }
}
