using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Benefit;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class BenefitServiceTests
{
    private Mock<IBenefitRepository>      _mockBenefitRepo;
    private Mock<IEmployeeRepository>     _mockEmployeeRepo;
    private Mock<INotificationService>    _mockNotificationService;
    private Mock<IUserAccountRepository>  _mockUserRepo;
    private IBenefitService               _benefitService;

    private Employee    _testEmployee = null!;
    private Benefit     _allowanceBenefit = null!;
    private Benefit     _deductionBenefit = null!;

    [SetUp]
    public void SetUp()
    {
        _mockBenefitRepo         = new Mock<IBenefitRepository>();
        _mockEmployeeRepo        = new Mock<IEmployeeRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUserRepo            = new Mock<IUserAccountRepository>();

        _benefitService = new BenefitService(
            _mockBenefitRepo.Object,
            _mockEmployeeRepo.Object,
            _mockNotificationService.Object,
            _mockUserRepo.Object);

        _testEmployee = new Employee
        {
            EmployeeId=1, FirstName="Arun", LastName="K",
            Department="IT", Designation="Developer",
            BasicSalary=60000, IsActive=true, JoinDate=DateTime.Now, Email="arun@test.com"
        };

        _allowanceBenefit = new Benefit
        {
            BenefitId=1, BenefitName="Bonus",
            BenefitType="Allowance", Description="Performance bonus"
        };

        _deductionBenefit = new Benefit
        {
            BenefitId=2, BenefitName="PF",
            BenefitType="Deduction", Description="Provident fund"
        };
    }

    // ── GetAllAsync ───────────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ReturnsBenefitsWithType()
    {
        _mockBenefitRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Benefit> { _allowanceBenefit, _deductionBenefit });

        var result = (await _benefitService.GetAllAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].BenefitType, Is.EqualTo("Allowance"));
        Assert.That(result[1].BenefitType, Is.EqualTo("Deduction"));
    }

    // ── CreateAsync ───────────────────────────────────────────
    [TestCase("Allowance")]
    [TestCase("Deduction")]
    public async Task CreateAsync_WithValidType_CreatesBenefit(string benefitType)
    {
        var dto = new CreateBenefitDto
        {
            BenefitName = "Test Benefit",
            Description = "Test",
            BenefitType = benefitType
        };
        var created = new Benefit
        {
            BenefitId=10, BenefitName=dto.BenefitName,
            Description=dto.Description, BenefitType=benefitType
        };

        _mockBenefitRepo.Setup(r => r.AddAsync(It.IsAny<Benefit>())).ReturnsAsync(created);

        var result = await _benefitService.CreateAsync(dto);

        Assert.That(result.BenefitType, Is.EqualTo(benefitType));
        Assert.That(result.BenefitName, Is.EqualTo("Test Benefit"));
    }

    // ── AssignBenefitAsync — Amount stored ───────────────────
    [Test]
    public async Task AssignBenefitAsync_WithAmount_StoresAmountCorrectly()
    {
        var dto = new AssignBenefitDto
        {
            EmployeeId=1, BenefitId=1, Amount=5000
        };
        var created = new EmployeeBenefit
        {
            EmployeeBenefitId=10, EmployeeId=1, BenefitId=1,
            Amount=5000, Employee=_testEmployee, Benefit=_allowanceBenefit
        };
        var userAccount = new UserAccount
        {
            UserId=5, EmployeeId=1, Username="arunk", RoleName="Employee", PasswordHash="x"
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockBenefitRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_allowanceBenefit);
        _mockBenefitRepo.Setup(r => r.BenefitAssignedAsync(1, 1)).ReturnsAsync(false);
        _mockBenefitRepo.Setup(r => r.AssignBenefitAsync(It.IsAny<EmployeeBenefit>()))
                         .ReturnsAsync(created);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync(userAccount);
        _mockNotificationService.Setup(s => s.NotifyAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                                 .Returns(Task.CompletedTask);

        var result = await _benefitService.AssignBenefitAsync(dto);

        Assert.That(result.Amount, Is.EqualTo(5000));
        Assert.That(result.BenefitType, Is.EqualTo("Allowance"));
        Assert.That(result.BenefitName, Is.EqualTo("Bonus"));

        _mockNotificationService.Verify(
            s => s.NotifyAsync(5, It.Is<string>(t => t.Contains("Benefit Assigned")), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public void AssignBenefitAsync_WhenAlreadyAssigned_ThrowsInvalidOperationException()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockBenefitRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_allowanceBenefit);
        _mockBenefitRepo.Setup(r => r.BenefitAssignedAsync(1, 1)).ReturnsAsync(true);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _benefitService.AssignBenefitAsync(
                new AssignBenefitDto { EmployeeId=1, BenefitId=1, Amount=3000 }));
    }

    // ── GetEmployeeBenefitSummaryAsync ────────────────────────
    [Test]
    public async Task GetEmployeeBenefitSummaryAsync_CorrectlySumsByType()
    {
        var empBenefits = new List<EmployeeBenefit>
        {
            new() { EmployeeBenefitId=1, EmployeeId=1, BenefitId=1, Amount=5000, Employee=_testEmployee, Benefit=_allowanceBenefit },
            new() { EmployeeBenefitId=2, EmployeeId=1, BenefitId=2, Amount=1800, Employee=_testEmployee, Benefit=_deductionBenefit }
        };

        _mockBenefitRepo.Setup(r => r.GetEmployeeBenefitsAsync(1)).ReturnsAsync(empBenefits);
        _mockBenefitRepo.Setup(r => r.GetBenefitTotalsAsync(1)).ReturnsAsync((5000m, 1800m));

        var result = await _benefitService.GetEmployeeBenefitSummaryAsync(1);

        Assert.That(result.TotalAllowance, Is.EqualTo(5000));
        Assert.That(result.TotalDeduction, Is.EqualTo(1800));
        Assert.That(result.Benefits.Count, Is.EqualTo(2));
    }

    // ── Amount TestCase ───────────────────────────────────────
    [TestCase(1000)]
    [TestCase(5000)]
    [TestCase(12500)]
    [TestCase(0.01)]
    public async Task AssignBenefit_VariousAmounts_AreStoredCorrectly(decimal amount)
    {
        var dto = new AssignBenefitDto { EmployeeId=1, BenefitId=1, Amount=amount };
        var created = new EmployeeBenefit
        {
            EmployeeBenefitId=1, EmployeeId=1, BenefitId=1,
            Amount=amount, Employee=_testEmployee, Benefit=_allowanceBenefit
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockBenefitRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_allowanceBenefit);
        _mockBenefitRepo.Setup(r => r.BenefitAssignedAsync(1, 1)).ReturnsAsync(false);
        _mockBenefitRepo.Setup(r => r.AssignBenefitAsync(It.IsAny<EmployeeBenefit>())).ReturnsAsync(created);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync((UserAccount?)null);

        var result = await _benefitService.AssignBenefitAsync(dto);

        Assert.That(result.Amount, Is.EqualTo(amount));
    }
}
