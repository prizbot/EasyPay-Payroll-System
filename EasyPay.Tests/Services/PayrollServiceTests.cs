using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Payroll;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class PayrollServiceTests
{
    private Mock<IPayrollRepository>     _mockPayrollRepo;
    private Mock<IEmployeeRepository>    _mockEmployeeRepo;
    private Mock<IBenefitRepository>     _mockBenefitRepo;
    private Mock<IAuditService>          _mockAuditService;
    private Mock<INotificationService>   _mockNotificationService;
    private Mock<IUserAccountRepository> _mockUserRepo;
    private IPayrollService              _payrollService;

    private Employee _testEmployee = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPayrollRepo         = new Mock<IPayrollRepository>();
        _mockEmployeeRepo        = new Mock<IEmployeeRepository>();
        _mockBenefitRepo         = new Mock<IBenefitRepository>();
        _mockAuditService        = new Mock<IAuditService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUserRepo            = new Mock<IUserAccountRepository>();

        _payrollService = new PayrollService(
            _mockPayrollRepo.Object,
            _mockEmployeeRepo.Object,
            _mockBenefitRepo.Object,
            _mockAuditService.Object,
            _mockNotificationService.Object,
            _mockUserRepo.Object);

        _testEmployee = new Employee
        {
            EmployeeId  = 1, FirstName = "Priya", LastName = "R",
            Department  = "IT", Designation = "Developer",
            BasicSalary = 60000, IsActive = true,
            JoinDate    = DateTime.Now, Email = "priya@test.com"
        };
    }

    // ── GetAllAsync ───────────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ReturnsAllPayrolls()
    {
        var payrolls = new List<Payroll>
        {
            new() { PayrollId=1, EmployeeId=1, PayMonth=6, PayYear=2026,
                    BasicSalary=60000, Allowance=5000, Deduction=1800,
                    NetSalary=63200, Employee=_testEmployee }
        };
        _mockPayrollRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(payrolls);

        var result = (await _payrollService.GetAllAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].NetSalary, Is.EqualTo(63200));
        _mockPayrollRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ── GeneratePayrollAsync — explicit values ────────────────
    [Test]
    public async Task GeneratePayrollAsync_WithExplicitAllowanceDeduction_CalculatesCorrectly()
    {
        var dto = new GeneratePayrollDto
        {
            EmployeeId=1, PayMonth=7, PayYear=2026, Allowance=5000, Deduction=2000
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockPayrollRepo.Setup(r => r.GetByEmployeeMonthYearAsync(1, 7, 2026))
                         .ReturnsAsync((Payroll?)null);
        _mockPayrollRepo.Setup(r => r.AddAsync(It.IsAny<Payroll>()))
                         .ReturnsAsync((Payroll p) => { p.Employee = _testEmployee; return p; });
        _mockPayrollRepo.Setup(r => r.AddPayStubAsync(It.IsAny<PayStub>())).Returns(Task.CompletedTask);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync((UserAccount?)null);

        var result = await _payrollService.GeneratePayrollAsync(dto);

        // 60000 + 5000 - 2000 = 63000
        Assert.That(result.NetSalary, Is.EqualTo(63000));
        Assert.That(result.Allowance, Is.EqualTo(5000));
        Assert.That(result.Deduction, Is.EqualTo(2000));

        // Verify benefit repo was NOT called when explicit values given
        _mockBenefitRepo.Verify(r => r.GetBenefitTotalsAsync(It.IsAny<int>()), Times.Never);
    }

    // ── Auto-calculate from benefits when both = 0 ────────────
    [Test]
    public async Task GeneratePayrollAsync_WhenAllowanceAndDeductionAreZero_AutoCalculatesFromBenefits()
    {
        var dto = new GeneratePayrollDto
        {
            EmployeeId=1, PayMonth=8, PayYear=2026,
            Allowance=0, Deduction=0   // ← triggers auto-calc
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockPayrollRepo.Setup(r => r.GetByEmployeeMonthYearAsync(1, 8, 2026))
                         .ReturnsAsync((Payroll?)null);
        // Benefits: Bonus=5000 (allowance), PF=1800 (deduction)
        _mockBenefitRepo.Setup(r => r.GetBenefitTotalsAsync(1))
                         .ReturnsAsync((5000m, 1800m));
        _mockPayrollRepo.Setup(r => r.AddAsync(It.IsAny<Payroll>()))
                         .ReturnsAsync((Payroll p) => { p.Employee = _testEmployee; return p; });
        _mockPayrollRepo.Setup(r => r.AddPayStubAsync(It.IsAny<PayStub>())).Returns(Task.CompletedTask);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync((UserAccount?)null);

        var result = await _payrollService.GeneratePayrollAsync(dto);

        // 60000 + 5000 - 1800 = 63200
        Assert.That(result.NetSalary, Is.EqualTo(63200));
        Assert.That(result.Allowance, Is.EqualTo(5000));
        Assert.That(result.Deduction, Is.EqualTo(1800));

        _mockBenefitRepo.Verify(r => r.GetBenefitTotalsAsync(1), Times.Once);
    }

    // ── GetBenefitTotalsForEmployee ───────────────────────────
    [Test]
    public async Task GetBenefitTotalsForEmployeeAsync_ReturnsSummedTotals()
    {
        _mockBenefitRepo.Setup(r => r.GetBenefitTotalsAsync(1))
                         .ReturnsAsync((5000m, 1800m));

        var result = await _payrollService.GetBenefitTotalsForEmployeeAsync(1);

        Assert.That(result.TotalAllowance, Is.EqualTo(5000));
        Assert.That(result.TotalDeduction, Is.EqualTo(1800));
    }

    // ── Notification sent after generation ───────────────────
    [Test]
    public async Task GeneratePayrollAsync_SendsNotificationToEmployee()
    {
        var dto = new GeneratePayrollDto
        {
            EmployeeId=1, PayMonth=9, PayYear=2026, Allowance=3000, Deduction=1000
        };
        var userAccount = new UserAccount
        {
            UserId=7, EmployeeId=1, Username="priya", RoleName="Employee", PasswordHash="x"
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockPayrollRepo.Setup(r => r.GetByEmployeeMonthYearAsync(1, 9, 2026))
                         .ReturnsAsync((Payroll?)null);
        _mockPayrollRepo.Setup(r => r.AddAsync(It.IsAny<Payroll>()))
                         .ReturnsAsync((Payroll p) => { p.Employee = _testEmployee; return p; });
        _mockPayrollRepo.Setup(r => r.AddPayStubAsync(It.IsAny<PayStub>())).Returns(Task.CompletedTask);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync(userAccount);
        _mockNotificationService.Setup(s => s.NotifyAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                                 .Returns(Task.CompletedTask);

        await _payrollService.GeneratePayrollAsync(dto);

        _mockNotificationService.Verify(
            s => s.NotifyAsync(
                7,
                It.Is<string>(t => t.Contains("Payroll Generated")),
                It.IsAny<string>()),
            Times.Once);
    }

    // ── Duplicate payroll ─────────────────────────────────────
    [Test]
    public void GeneratePayrollAsync_WhenPayrollAlreadyExists_ThrowsInvalidOperationException()
    {
        var existing = new Payroll { PayrollId=1, EmployeeId=1, PayMonth=6, PayYear=2026, NetSalary=60000 };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockPayrollRepo.Setup(r => r.GetByEmployeeMonthYearAsync(1, 6, 2026)).ReturnsAsync(existing);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _payrollService.GeneratePayrollAsync(
                new GeneratePayrollDto { EmployeeId=1, PayMonth=6, PayYear=2026 }));
    }

    // ── Employee not found ────────────────────────────────────
    [Test]
    public void GeneratePayrollAsync_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Employee?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _payrollService.GeneratePayrollAsync(
                new GeneratePayrollDto { EmployeeId=999, PayMonth=6, PayYear=2026 }));
    }

    // ── Net salary negative ───────────────────────────────────
    [Test]
    public void GeneratePayrollAsync_WhenNetSalaryNegative_ThrowsArgumentException()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockPayrollRepo.Setup(r => r.GetByEmployeeMonthYearAsync(1, 7, 2026))
                         .ReturnsAsync((Payroll?)null);

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _payrollService.GeneratePayrollAsync(
                new GeneratePayrollDto { EmployeeId=1, PayMonth=7, PayYear=2026,
                                          Allowance=0, Deduction=999999 }));
    }

    // ── NetSalary calculation TestCase ───────────────────────
    [TestCase(60000, 5000, 1800, 63200)]
    [TestCase(60000, 0,    0,    60000)]
    [TestCase(60000, 10000,3000, 67000)]
    [TestCase(45000, 4000, 1500, 47500)]
    public async Task GeneratePayroll_NetSalaryCalculation_IsCorrect(
        decimal basic, decimal allowance, decimal deduction, decimal expectedNet)
    {
        var emp = new Employee
        {
            EmployeeId=1, FirstName="T", LastName="U", Department="IT",
            Designation="Dev", BasicSalary=basic, IsActive=true,
            JoinDate=DateTime.Now, Email=$"t{basic}@u.com"
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(emp);
        _mockPayrollRepo.Setup(r => r.GetByEmployeeMonthYearAsync(1, It.IsAny<int>(), It.IsAny<int>()))
                         .ReturnsAsync((Payroll?)null);
        _mockPayrollRepo.Setup(r => r.AddAsync(It.IsAny<Payroll>()))
                         .ReturnsAsync((Payroll p) => { p.Employee = emp; return p; });
        _mockPayrollRepo.Setup(r => r.AddPayStubAsync(It.IsAny<PayStub>())).Returns(Task.CompletedTask);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(It.IsAny<int>())).ReturnsAsync((UserAccount?)null);

        var dto    = new GeneratePayrollDto { EmployeeId=1, PayMonth=6, PayYear=2026, Allowance=allowance, Deduction=deduction };
        var result = await _payrollService.GeneratePayrollAsync(dto);

        Assert.That(result.NetSalary, Is.EqualTo(expectedNet));
    }
}
