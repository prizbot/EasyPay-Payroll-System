using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Attendance;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class AttendanceServiceTests
{
    private Mock<IAttendanceRepository> _mockAttendanceRepo;
    private Mock<IEmployeeRepository>   _mockEmployeeRepo;
    private Mock<ILeaveRepository>      _mockLeaveRepo;
    private IAttendanceService          _attendanceService;
    private Employee                    _testEmployee = null!;

    [SetUp]
    public void SetUp()
    {
        _mockAttendanceRepo = new Mock<IAttendanceRepository>();
        _mockEmployeeRepo   = new Mock<IEmployeeRepository>();
        _mockLeaveRepo      = new Mock<ILeaveRepository>();

        _attendanceService  = new AttendanceService(
            _mockAttendanceRepo.Object,
            _mockEmployeeRepo.Object,
            _mockLeaveRepo.Object);

        _testEmployee = new Employee
        {
            EmployeeId=1, FirstName="Priya", LastName="R",
            Department="IT", Designation="Developer",
            BasicSalary=50000, IsActive=true, JoinDate=DateTime.Now, Email="priya@test.com"
        };
    }

    // ── GetAllAsync ───────────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ReturnsAllRecords()
    {
        var records = new List<Attendance>
        {
            new() { AttendanceId=1, EmployeeId=1, AttendanceDate=DateTime.Today, Status="Present", Employee=_testEmployee },
            new() { AttendanceId=2, EmployeeId=1, AttendanceDate=DateTime.Today.AddDays(-1), Status="Absent", Employee=_testEmployee }
        };
        _mockAttendanceRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(records);

        var result = (await _attendanceService.GetAllAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Status, Is.EqualTo("Present"));
        _mockAttendanceRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ── MarkAttendanceAsync — Happy Path ──────────────────────
    [Test]
    public async Task MarkAttendanceAsync_NoExistingMark_NoApprovedLeave_Succeeds()
    {
        var dto = new CreateAttendanceDto
        {
            EmployeeId=1, AttendanceDate=DateTime.Today, Status="Present"
        };
        var created = new Attendance
        {
            AttendanceId=10, EmployeeId=1,
            AttendanceDate=DateTime.Today, Status="Present", Employee=_testEmployee
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockAttendanceRepo.Setup(r => r.ExistsAsync(1, dto.AttendanceDate)).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.HasApprovedLeaveOnDateAsync(1, dto.AttendanceDate)).ReturnsAsync(false);
        _mockAttendanceRepo.Setup(r => r.AddAsync(It.IsAny<Attendance>())).ReturnsAsync(created);

        var result = await _attendanceService.MarkAttendanceAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo("Present"));
        Assert.That(result.EmployeeName, Is.EqualTo("Priya R"));
        _mockAttendanceRepo.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Once);
    }

    // ── Attendance blocked during approved leave ───────────────
    [Test]
    public void MarkAttendanceAsync_WhenOnApprovedLeave_ThrowsInvalidOperationException()
    {
        var dto = new CreateAttendanceDto
        {
            EmployeeId=1, AttendanceDate=DateTime.Today, Status="Present"
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockAttendanceRepo.Setup(r => r.ExistsAsync(1, dto.AttendanceDate)).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.HasApprovedLeaveOnDateAsync(1, dto.AttendanceDate))
                      .ReturnsAsync(true);   // ← approved leave exists

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _attendanceService.MarkAttendanceAsync(dto));

        Assert.That(ex!.Message, Is.EqualTo("Attendance cannot be marked during an approved leave period."));
        _mockAttendanceRepo.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Never);
    }

    [Test]
    public void MarkAttendanceAsync_WhenAlreadyMarked_ThrowsInvalidOperationException()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockAttendanceRepo.Setup(r => r.ExistsAsync(1, DateTime.Today)).ReturnsAsync(true);

        var dto = new CreateAttendanceDto { EmployeeId=1, AttendanceDate=DateTime.Today, Status="Present" };

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _attendanceService.MarkAttendanceAsync(dto));
    }

    [Test]
    public void MarkAttendanceAsync_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Employee?)null);

        var dto = new CreateAttendanceDto { EmployeeId=999, AttendanceDate=DateTime.Today, Status="Present" };

        Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _attendanceService.MarkAttendanceAsync(dto));
    }

    // ── GetSummaryAsync ───────────────────────────────────────
    [Test]
    public async Task GetSummaryAsync_CorrectlyCounts_EachStatus()
    {
        var records = new List<Attendance>
        {
            new() { Status="Present" }, new() { Status="Present" }, new() { Status="Present" },
            new() { Status="Absent" }, new() { Status="Half Day" }, new() { Status="Leave" }
        };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockAttendanceRepo.Setup(r => r.GetByEmployeeAndMonthAsync(1, 6, 2026)).ReturnsAsync(records);

        var result = await _attendanceService.GetSummaryAsync(1, 6, 2026);

        Assert.That(result.PresentDays, Is.EqualTo(3));
        Assert.That(result.AbsentDays,  Is.EqualTo(1));
        Assert.That(result.HalfDays,    Is.EqualTo(1));
        Assert.That(result.LeaveDays,   Is.EqualTo(1));
        Assert.That(result.TotalDays,   Is.EqualTo(6));
    }

    // ── Status TestCase ───────────────────────────────────────
    [TestCase("Present")]
    [TestCase("Absent")]
    [TestCase("Half Day")]
    [TestCase("Leave")]
    public async Task MarkAttendance_AcceptsAllValidStatuses(string status)
    {
        var dto     = new CreateAttendanceDto { EmployeeId=1, AttendanceDate=DateTime.Today, Status=status };
        var created = new Attendance { AttendanceId=1, EmployeeId=1, AttendanceDate=DateTime.Today, Status=status, Employee=_testEmployee };

        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_testEmployee);
        _mockAttendanceRepo.Setup(r => r.ExistsAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _mockLeaveRepo.Setup(r => r.HasApprovedLeaveOnDateAsync(1, It.IsAny<DateTime>())).ReturnsAsync(false);
        _mockAttendanceRepo.Setup(r => r.AddAsync(It.IsAny<Attendance>())).ReturnsAsync(created);

        var result = await _attendanceService.MarkAttendanceAsync(dto);

        Assert.That(result.Status, Is.EqualTo(status));
    }
}
