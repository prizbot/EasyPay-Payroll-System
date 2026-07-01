using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Leave;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class LeaveServiceTests
{
    private Mock<ILeaveRepository> _mockLeaveRepo;
    private Mock<ILeaveTypeRepository> _mockLeaveTypeRepo;
    private Mock<IEmployeeRepository> _mockEmployeeRepo;
    private Mock<IAuditService> _mockAuditService;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<IUserAccountRepository> _mockUserRepo;
    private ILeaveService _leaveService;

    private Employee _testEmployee = null!;
    private LeaveType _casualType = null!;
    private LeaveType _lopType = null!;
    private LeaveType _inactiveType = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLeaveRepo = new Mock<ILeaveRepository>();
        _mockLeaveTypeRepo = new Mock<ILeaveTypeRepository>();
        _mockEmployeeRepo = new Mock<IEmployeeRepository>();
        _mockAuditService = new Mock<IAuditService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUserRepo = new Mock<IUserAccountRepository>();

        _leaveService = new LeaveService(
            _mockLeaveRepo.Object,
            _mockLeaveTypeRepo.Object,
            _mockEmployeeRepo.Object,
            _mockAuditService.Object,
            _mockNotificationService.Object,
            _mockUserRepo.Object);

        _testEmployee = new Employee
        {
            EmployeeId = 1,
            FirstName = "Priya",
            LastName = "R",
            Department = "IT",
            Designation = "Developer",
            BasicSalary = 50000,
            IsActive = true,
            JoinDate = DateTime.Now,
            Email = "priya@test.com"
        };

        _casualType = new LeaveType
        {
            LeaveTypeId = 1,
            Name = "Casual",
            IsPaid = true,
            AnnualAllowance = 12,
            IsActive = true
        };

        _lopType = new LeaveType
        {
            LeaveTypeId = 8,
            Name = "LOP",
            IsPaid = false,
            AnnualAllowance = 0,
            IsActive = true
        };

        _inactiveType = new LeaveType
        {
            LeaveTypeId = 9,
            Name = "OldLeave",
            IsPaid = true,
            AnnualAllowance = 5,
            IsActive = false
        };
    }

    // ── GetAllAsync ───────────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ReturnsAllLeaveRequests()
    {
        var leaves = new List<LeaveRequest>
        {
            MakeLeave(1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), _casualType),
            MakeLeave(2, DateTime.Today.AddDays(5), DateTime.Today.AddDays(6), _lopType)
        };
        _mockLeaveRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(leaves);

        var result = (await _leaveService.GetAllAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].LeaveTypeName, Is.EqualTo("Casual"));
        Assert.That(result[1].LeaveTypeName, Is.EqualTo("LOP"));
        _mockLeaveRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ── Past date validation ──────────────────────────────────
    [Test]
    public void SubmitLeaveAsync_WhenStartDateIsInPast_ThrowsInvalidOperationException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(-1),   // ← yesterday
            EndDate = DateTime.Today.AddDays(1),
            LeaveTypeId = 1
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));

        Assert.That(ex!.Message, Is.EqualTo("Leave cannot be requested for past dates."));
        _mockLeaveRepo.Verify(r => r.AddAsync(It.IsAny<LeaveRequest>()), Times.Never);
    }

    [Test]
    public void SubmitLeaveAsync_WhenStartDateIsMoreThanOneDayInPast_ThrowsInvalidOperationException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(-5),
            EndDate = DateTime.Today.AddDays(-1),
            LeaveTypeId = 1
        };

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    [Test]
    public async Task SubmitLeaveAsync_WhenStartDateIsToday_Succeeds()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today,               // ← today is allowed
            EndDate = DateTime.Today.AddDays(2),
            LeaveTypeId = 1
        };
        var created = MakeLeave(5, dto.StartDate, dto.EndDate, _casualType);

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_casualType);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, dto.StartDate, dto.EndDate, null)).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.AddAsync(It.IsAny<LeaveRequest>())).ReturnsAsync(created);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task SubmitLeaveAsync_WhenStartDateIsFuture_Succeeds()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(3),
            EndDate = DateTime.Today.AddDays(5),
            LeaveTypeId = 1
        };
        var created = MakeLeave(6, dto.StartDate, dto.EndDate, _casualType);

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_casualType);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, dto.StartDate, dto.EndDate, null)).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.AddAsync(It.IsAny<LeaveRequest>())).ReturnsAsync(created);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result, Is.Not.Null);
    }

    // ── End date before start date ────────────────────────────
    [Test]
    public void SubmitLeaveAsync_WhenEndDateBeforeStartDate_ThrowsArgumentException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(2),    // ← before start
            LeaveTypeId = 1
        };

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    // ── Leave type validation ─────────────────────────────────
    [Test]
    public void SubmitLeaveAsync_WhenLeaveTypeNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2),
            LeaveTypeId = 999   // ← does not exist
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((LeaveType?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    [Test]
    public void SubmitLeaveAsync_WhenLeaveTypeIsInactive_ThrowsInvalidOperationException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(2),
            LeaveTypeId = 9   // ← inactive
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(_inactiveType);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));

        Assert.That(ex!.Message, Does.Contain("no longer active"));
    }

    // ── Paid / Unpaid stored correctly ────────────────────────
    [Test]
    public async Task SubmitLeaveAsync_WithPaidLeaveType_ReturnsDtoWithIsPaidTrue()
    {
        var dto = MakeFutureDto(1);   // Casual (IsPaid = true)
        var created = MakeLeave(10, dto.StartDate, dto.EndDate, _casualType);

        SetupHappyPath(dto, created, _casualType);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result.IsPaid, Is.True);
        Assert.That(result.LeaveTypeName, Is.EqualTo("Casual"));
    }

    [Test]
    public async Task SubmitLeaveAsync_WithUnpaidLeaveType_ReturnsDtoWithIsPaidFalse()
    {
        var dto = MakeFutureDto(8);   // LOP (IsPaid = false)
        var created = MakeLeave(11, dto.StartDate, dto.EndDate, _lopType);

        SetupHappyPath(dto, created, _lopType);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result.IsPaid, Is.False);
        Assert.That(result.LeaveTypeName, Is.EqualTo("LOP"));
    }

    // ── Overlap validation ────────────────────────────────────
    [Test]
    public void SubmitLeaveAsync_WhenOverlapExists_ThrowsInvalidOperationException()
    {
        var dto = MakeFutureDto(1);

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_casualType);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, dto.StartDate, dto.EndDate, null))
                      .ReturnsAsync(true);   // ← overlap

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));

        Assert.That(ex!.Message, Does.Contain("overlaps"));
        _mockLeaveRepo.Verify(r => r.AddAsync(It.IsAny<LeaveRequest>()), Times.Never);
    }

    // ── Employee not found ────────────────────────────────────
    [Test]
    public void SubmitLeaveAsync_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        var dto = MakeFutureDto(1);
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Employee?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    // ── UpdateStatusAsync ─────────────────────────────────────
    [Test]
    public async Task UpdateStatusAsync_Approve_SendsNotification()
    {
        var leave = MakeLeave(3, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), _casualType);
        var userAccount = new UserAccount { UserId = 10, EmployeeId = 1, Username = "priya", RoleName = "Employee", PasswordHash = "x" };

        _mockLeaveRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(leave);
        _mockLeaveRepo.Setup(r => r.UpdateAsync(It.IsAny<LeaveRequest>()))
                      .ReturnsAsync((LeaveRequest l) => l);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync(userAccount);
        _mockNotificationService.Setup(s => s.NotifyAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                                 .Returns(Task.CompletedTask);

        var result = await _leaveService.UpdateStatusAsync(3, new UpdateLeaveStatusDto { Status = "Approved" }, 2);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("Approved"));
        _mockNotificationService.Verify(
            s => s.NotifyAsync(10, It.Is<string>(t => t.Contains("Approved")), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public void UpdateStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _leaveService.UpdateStatusAsync(1, new UpdateLeaveStatusDto { Status = "Maybe" }, 1));
    }

    [Test]
    public async Task UpdateStatusAsync_LeaveNotFound_ReturnsNull()
    {
        _mockLeaveRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((LeaveRequest?)null);

        var result = await _leaveService.UpdateStatusAsync(999, new UpdateLeaveStatusDto { Status = "Approved" }, 1);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void UpdateStatusAsync_LeaveAlreadyApproved_ThrowsInvalidOperationException()
    {
        var leave = MakeLeave(4, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), _casualType);
        leave.Status = "Approved";
        _mockLeaveRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(leave);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.UpdateStatusAsync(4, new UpdateLeaveStatusDto { Status = "Rejected" }, 1));
    }

    // ── TotalDays computed correctly ──────────────────────────
    [TestCase(1, 1, 1)]
    [TestCase(1, 3, 3)]
    [TestCase(1, 7, 7)]
    [TestCase(1, 14, 14)]
    public async Task SubmitLeave_TotalDays_IsCalculatedCorrectly(int startOffset, int endOffset, int expected)
    {
        var start = DateTime.Today.AddDays(startOffset);
        var end = DateTime.Today.AddDays(endOffset);
        var dto = new CreateLeaveRequestDto { EmployeeId = 1, StartDate = start, EndDate = end, LeaveTypeId = 1 };
        var created = MakeLeave(99, start, end, _casualType);

        SetupHappyPath(dto, created, _casualType);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result.TotalDays, Is.EqualTo(expected));
    }

    // ── Past date boundary TestCase ───────────────────────────
    [TestCase(-5)]
    [TestCase(-1)]
    [TestCase(-30)]
    public void SubmitLeave_PastStartDates_AllThrow(int daysOffset)
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            StartDate = DateTime.Today.AddDays(daysOffset),
            EndDate = DateTime.Today.AddDays(daysOffset + 1),
            LeaveTypeId = 1
        };

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    // ── Helpers ───────────────────────────────────────────────
    private CreateLeaveRequestDto MakeFutureDto(int leaveTypeId) => new()
    {
        EmployeeId = 1,
        StartDate = DateTime.Today.AddDays(2),
        EndDate = DateTime.Today.AddDays(4),
        LeaveTypeId = leaveTypeId
    };

    private LeaveRequest MakeLeave(int id, DateTime start, DateTime end, LeaveType lt) => new()
    {
        LeaveId = id,
        EmployeeId = 1,
        StartDate = start,
        EndDate = end,
        LeaveTypeId = lt.LeaveTypeId,
        LeaveType = lt.Name,
        Status = "Pending",
        Employee = _testEmployee,
        LeaveTypeNav = lt
    };

    private void SetupHappyPath(CreateLeaveRequestDto dto, LeaveRequest created, LeaveType lt)
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync(lt.LeaveTypeId)).ReturnsAsync(lt);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, dto.StartDate, dto.EndDate, null)).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.AddAsync(It.IsAny<LeaveRequest>())).ReturnsAsync(created);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
    }
}