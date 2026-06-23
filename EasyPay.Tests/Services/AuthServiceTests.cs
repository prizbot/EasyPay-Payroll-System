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
    private Mock<IJwtService>            _mockJwtService;
    private Mock<IAuditService>          _mockAuditService;
    private IAuthService                 _authService;

    private Employee    _testEmployee    = null!;
    private UserAccount _testUserAccount = null!;

    [SetUp]
    public void SetUp()
    {
        _mockUserRepo     = new Mock<IUserAccountRepository>();
        _mockJwtService   = new Mock<IJwtService>();
        _mockAuditService = new Mock<IAuditService>();

        _authService = new AuthService(
            _mockUserRepo.Object,
            _mockJwtService.Object,
            _mockAuditService.Object);

        _testEmployee = new Employee
        {
            EmployeeId  = 1,
            FirstName   = "Admin",
            LastName    = "User",
            Department  = "IT",
            Designation = "Admin",
            BasicSalary = 80000,
            IsActive    = true,
            JoinDate    = DateTime.Now,
            Email       = "admin@easypay.com"
        };

        _testUserAccount = new UserAccount
        {
            UserId       = 1,
            EmployeeId   = 1,
            Username     = "admin",
            PasswordHash = "admin123",   // plain text for seeded dev data path
            RoleName     = "Admin",
            Employee     = _testEmployee
        };
    }

    // ── LoginAsync ────────────────────────────────────────

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUserAccount);

        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>()))
            .Returns("fake-access-token");

        _mockJwtService.Setup(j => j.GenerateRefreshToken())
            .Returns("fake-refresh-token");

        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>()))
            .ReturnsAsync(_testUserAccount);

        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "admin123"
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.EqualTo("fake-access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("fake-refresh-token"));
        Assert.That(result.Role, Is.EqualTo("Admin"));
        Assert.That(result.FullName, Is.EqualTo("Admin User"));

        _mockJwtService.Verify(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>()), Times.Once);
        _mockAuditService.Verify(a => a.LogAsync(1, It.Is<string>(s => s.Contains("Login"))), Times.Once);
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(_testUserAccount);

        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "admin",
            Password = "wrongpassword"
        });

        Assert.That(result, Is.Null);
        _mockJwtService.Verify(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_WithNonExistentUsername_ReturnsNull()
    {
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("nobody"))
            .ReturnsAsync((UserAccount?)null);

        var result = await _authService.LoginAsync(new LoginRequestDto
        {
            Username = "nobody",
            Password = "anypassword"
        });

        Assert.That(result, Is.Null);
    }

    // ── RefreshTokenAsync ─────────────────────────────────

    [Test]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        var userWithToken = new UserAccount
        {
            UserId              = 1,
            EmployeeId          = 1,
            Username            = "admin",
            PasswordHash        = "hashed",
            RoleName            = "Admin",
            RefreshToken        = "valid-refresh-token",
            RefreshTokenExpiry  = DateTime.UtcNow.AddDays(5),
            Employee            = _testEmployee
        };

        _mockUserRepo.Setup(r => r.GetByRefreshTokenAsync("valid-refresh-token"))
            .ReturnsAsync(userWithToken);

        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>()))
            .Returns("new-access-token");

        _mockJwtService.Setup(j => j.GenerateRefreshToken())
            .Returns("new-refresh-token");

        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>()))
            .ReturnsAsync(userWithToken);

        var result = await _authService.RefreshTokenAsync("valid-refresh-token");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Token, Is.EqualTo("new-access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("new-refresh-token"));
    }

    [Test]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsNull()
    {
        var expiredUser = new UserAccount
        {
            UserId             = 1,
            RefreshToken       = "expired-token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1),   // expired
            Employee           = _testEmployee
        };

        _mockUserRepo.Setup(r => r.GetByRefreshTokenAsync("expired-token"))
            .ReturnsAsync(expiredUser);

        var result = await _authService.RefreshTokenAsync("expired-token");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsNull()
    {
        _mockUserRepo.Setup(r => r.GetByRefreshTokenAsync("bad-token"))
            .ReturnsAsync((UserAccount?)null);

        var result = await _authService.RefreshTokenAsync("bad-token");

        Assert.That(result, Is.Null);
    }

    // ── TestCase variants ─────────────────────────────────

    [TestCase("admin",   "admin123",   true)]
    [TestCase("admin",   "wrongpass",  false)]
    [TestCase("nobody",  "anypass",    false)]
    public async Task LoginAsync_VariousCredentials_ReturnsExpectedResult(
        string username, string password, bool expectSuccess)
    {
        if (username == "admin")
        {
            _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin"))
                .ReturnsAsync(_testUserAccount);
        }
        else
        {
            _mockUserRepo.Setup(r => r.GetByUsernameAsync(username))
                .ReturnsAsync((UserAccount?)null);
        }

        if (expectSuccess)
        {
            _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<UserAccount>(), It.IsAny<Employee>())).Returns("token");
            _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserAccount>())).ReturnsAsync(_testUserAccount);
            _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        var result = await _authService.LoginAsync(new LoginRequestDto { Username = username, Password = password });

        Assert.That(result != null, Is.EqualTo(expectSuccess));
    }
}
