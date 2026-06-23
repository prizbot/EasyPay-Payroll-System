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
    private Mock<ILeaveRepository>       _mockLeaveRepo;
    private Mock<IEmployeeRepository>    _mockEmployeeRepo;
    private Mock<IAuditService>          _mockAuditService;
    private Mock<INotificationService>   _mockNotificationService;
    private Mock<IUserAccountRepository> _mockUserRepo;
    private ILeaveService                _leaveService;
    private Employee                     _testEmployee = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLeaveRepo           = new Mock<ILeaveRepository>();
        _mockEmployeeRepo        = new Mock<IEmployeeRepository>();
        _mockAuditService        = new Mock<IAuditService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUserRepo            = new Mock<IUserAccountRepository>();

        _leaveService = new LeaveService(
            _mockLeaveRepo.Object,
            _mockEmployeeRepo.Object,
            _mockAuditService.Object,
            _mockNotificationService.Object,
            _mockUserRepo.Object);

        _testEmployee = new Employee
        {
            EmployeeId  = 1, FirstName = "Sneha", LastName = "P",
            Department  = "Testing", Designation = "QA", BasicSalary = 48000,
            IsActive    = true, JoinDate = DateTime.Now, Email = "sneha@test.com"
        };
    }

    // ── GetAllAsync ───────────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ReturnsAllLeaveRequests()
    {
        var leaves = new List<LeaveRequest>
        {
            new() { LeaveId=1, EmployeeId=1, StartDate=DateTime.Today, EndDate=DateTime.Today.AddDays(2), LeaveType="Casual", Status="Pending", Employee=_testEmployee },
            new() { LeaveId=2, EmployeeId=1, StartDate=DateTime.Today.AddDays(-5), EndDate=DateTime.Today.AddDays(-3), LeaveType="Sick", Status="Approved", Employee=_testEmployee }
        };
        _mockLeaveRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(leaves);

        var result = (await _leaveService.GetAllAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Status, Is.EqualTo("Pending"));
        _mockLeaveRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ── SubmitLeaveAsync — Happy Path ─────────────────────────
    [Test]
    public async Task SubmitLeaveAsync_WithValidDates_NoOverlap_CreatesPendingLeave()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId=1, StartDate=DateTime.Today.AddDays(5),
            EndDate=DateTime.Today.AddDays(7), LeaveType="Casual", Reason="Holiday"
        };
        var created = new LeaveRequest
        {
            LeaveId=5, EmployeeId=1, StartDate=dto.StartDate, EndDate=dto.EndDate,
            LeaveType=dto.LeaveType, Reason=dto.Reason, Status="Pending", Employee=_testEmployee
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, dto.StartDate.Date, dto.EndDate.Date, null))
                      .ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.AddAsync(It.IsAny<LeaveRequest>())).ReturnsAsync(created);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Pending"));
        Assert.That(result.TotalDays, Is.EqualTo(3));
        _mockLeaveRepo.Verify(r => r.AddAsync(It.IsAny<LeaveRequest>()), Times.Once);
    }

    // ── Leave Overlap Validation ──────────────────────────────
    [Test]
    public void SubmitLeaveAsync_WhenOverlapExists_ThrowsInvalidOperationException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId=1, StartDate=DateTime.Today.AddDays(1),
            EndDate=DateTime.Today.AddDays(3), LeaveType="Casual"
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, dto.StartDate.Date, dto.EndDate.Date, null))
                      .ReturnsAsync(true);   // ← overlap found

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));

        Assert.That(ex!.Message, Is.EqualTo("Leave request overlaps with an existing leave request."));
        _mockLeaveRepo.Verify(r => r.AddAsync(It.IsAny<LeaveRequest>()), Times.Never);
    }

    [Test]
    public void SubmitLeaveAsync_WhenEndDateBeforeStartDate_ThrowsArgumentException()
    {
        var dto = new CreateLeaveRequestDto
        {
            EmployeeId=1, StartDate=DateTime.Today.AddDays(5),
            EndDate=DateTime.Today.AddDays(2), LeaveType="Casual"
        };

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    [Test]
    public void SubmitLeaveAsync_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Employee?)null);

        var dto = new CreateLeaveRequestDto
        {
            EmployeeId=999, StartDate=DateTime.Today, EndDate=DateTime.Today.AddDays(1), LeaveType="Casual"
        };

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _leaveService.SubmitLeaveAsync(dto));
    }

    // ── UpdateStatusAsync ─────────────────────────────────────
    [Test]
    public async Task UpdateStatusAsync_Approve_UpdatesStatusAndSendsNotification()
    {
        var leave = new LeaveRequest
        {
            LeaveId=3, EmployeeId=1, StartDate=DateTime.Today,
            EndDate=DateTime.Today.AddDays(1), LeaveType="Sick",
            Status="Pending", Employee=_testEmployee
        };
        var userAccount = new UserAccount { UserId=10, EmployeeId=1, Username="sneha", RoleName="Employee", PasswordHash="x" };

        _mockLeaveRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(leave);
        _mockLeaveRepo.Setup(r => r.UpdateAsync(It.IsAny<LeaveRequest>())).ReturnsAsync((LeaveRequest l) => l);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetByEmployeeIdAsync(1)).ReturnsAsync(userAccount);
        _mockNotificationService.Setup(s => s.NotifyAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                                 .Returns(Task.CompletedTask);

        var result = await _leaveService.UpdateStatusAsync(3, new UpdateLeaveStatusDto { Status="Approved" }, approvedByUserId: 2);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("Approved"));
        _mockNotificationService.Verify(
            s => s.NotifyAsync(10, It.Is<string>(t => t.Contains("Approved")), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public void UpdateStatusAsync_WhenLeaveAlreadyApproved_ThrowsInvalidOperationException()
    {
        var leave = new LeaveRequest
        {
            LeaveId=5, Status="Approved", Employee=_testEmployee,
            StartDate=DateTime.Today, EndDate=DateTime.Today.AddDays(1)
        };
        _mockLeaveRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(leave);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _leaveService.UpdateStatusAsync(5, new UpdateLeaveStatusDto { Status="Rejected" }, 1));
    }

    [Test]
    public void UpdateStatusAsync_WithInvalidStatus_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _leaveService.UpdateStatusAsync(1, new UpdateLeaveStatusDto { Status="Maybe" }, 1));
    }

    // ── TotalDays TestCase ────────────────────────────────────
    [TestCase(1, 1, 1)]
    [TestCase(1, 3, 3)]
    [TestCase(1, 7, 7)]
    [TestCase(1, 14, 14)]
    public async Task SubmitLeave_TotalDays_IsCalculatedCorrectly(int startOffset, int endOffset, int expectedDays)
    {
        var start = DateTime.Today.AddDays(startOffset);
        var end   = DateTime.Today.AddDays(endOffset);
        var dto   = new CreateLeaveRequestDto { EmployeeId=1, StartDate=start, EndDate=end, LeaveType="Casual" };
        var created = new LeaveRequest
        {
            LeaveId=99, EmployeeId=1, StartDate=start, EndDate=end,
            Status="Pending", Employee=_testEmployee
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockLeaveRepo.Setup(r => r.HasOverlapAsync(1, start, end, null)).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.AddAsync(It.IsAny<LeaveRequest>())).ReturnsAsync(created);
        _mockAuditService.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _leaveService.SubmitLeaveAsync(dto);

        Assert.That(result.TotalDays, Is.EqualTo(expectedDays));
    }
}
