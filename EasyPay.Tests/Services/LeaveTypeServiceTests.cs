using Moq;
using NUnit.Framework;
using EasyPay.Core.DTOs.Leave;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class LeaveTypeServiceTests
{
    private Mock<ILeaveTypeRepository> _mockRepo;
    private ILeaveTypeService _service;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<ILeaveTypeRepository>();
        _service = new LeaveTypeService(_mockRepo.Object);
    }

    // ── GetAllAsync ───────────────────────────────────────────
    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LeaveType>
        {
            new() { LeaveTypeId=1, Name="Casual",  IsPaid=true,  AnnualAllowance=12, IsActive=true },
            new() { LeaveTypeId=8, Name="LOP",     IsPaid=false, AnnualAllowance=0,  IsActive=true },
            new() { LeaveTypeId=9, Name="OldLeave",IsPaid=true,  AnnualAllowance=5,  IsActive=false }
        });

        var result = (await _service.GetAllAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[0].Name, Is.EqualTo("Casual"));
        Assert.That(result[1].IsPaid, Is.False);
        Assert.That(result[2].IsActive, Is.False);
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ── GetActiveAsync ────────────────────────────────────────
    [Test]
    public async Task GetActiveAsync_ReturnsOnlyActiveTypes()
    {
        _mockRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<LeaveType>
        {
            new() { LeaveTypeId=1, Name="Casual", IsPaid=true,  AnnualAllowance=12, IsActive=true },
            new() { LeaveTypeId=2, Name="Sick",   IsPaid=true,  AnnualAllowance=8,  IsActive=true }
        });

        var result = (await _service.GetActiveAsync()).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(r => r.IsActive), Is.True);
    }

    [Test]
    public async Task GetActiveAsync_WhenNoActiveTypes_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<LeaveType>());

        var result = await _service.GetActiveAsync();

        Assert.That(result, Is.Empty);
    }

    // ── GetByIdAsync ──────────────────────────────────────────
    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(
            new LeaveType { LeaveTypeId = 1, Name = "Casual", IsPaid = true, AnnualAllowance = 12, IsActive = true });

        var result = await _service.GetByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LeaveTypeId, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Casual"));
        Assert.That(result.IsPaid, Is.True);
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((LeaveType?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    // ── CreateAsync ───────────────────────────────────────────
    [Test]
    public async Task CreateAsync_WithPaidType_StoresPaidFlag()
    {
        var dto = new CreateLeaveTypeDto
        {
            Name = "Marriage Leave",
            IsPaid = true,
            AnnualAllowance = 5,
            Description = "For own marriage",
            IsActive = true
        };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType lt) => { lt.LeaveTypeId = 10; return lt; });

        var result = await _service.CreateAsync(dto);

        Assert.That(result.Name, Is.EqualTo("Marriage Leave"));
        Assert.That(result.IsPaid, Is.True);
        Assert.That(result.AnnualAllowance, Is.EqualTo(5));
        Assert.That(result.IsActive, Is.True);
        _mockRepo.Verify(r => r.AddAsync(It.Is<LeaveType>(lt =>
            lt.IsPaid == true && lt.Name == "Marriage Leave")), Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithUnpaidType_StoresUnpaidFlag()
    {
        var dto = new CreateLeaveTypeDto
        {
            Name = "LOP",
            IsPaid = false,
            AnnualAllowance = 0,
            Description = "Loss of Pay",
            IsActive = true
        };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType lt) => { lt.LeaveTypeId = 8; return lt; });

        var result = await _service.CreateAsync(dto);

        Assert.That(result.IsPaid, Is.False);
        Assert.That(result.AnnualAllowance, Is.EqualTo(0));
        _mockRepo.Verify(r => r.AddAsync(It.Is<LeaveType>(lt => lt.IsPaid == false)), Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithInactiveFlag_StoresInactive()
    {
        var dto = new CreateLeaveTypeDto
        {
            Name = "Deprecated Leave",
            IsPaid = true,
            AnnualAllowance = 0,
            IsActive = false
        };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType lt) => lt);

        var result = await _service.CreateAsync(dto);

        Assert.That(result.IsActive, Is.False);
    }

    // ── UpdateAsync ───────────────────────────────────────────
    [Test]
    public async Task UpdateAsync_WhenExists_UpdatesAllFields()
    {
        var existing = new LeaveType
        {
            LeaveTypeId = 1,
            Name = "Casual",
            IsPaid = true,
            AnnualAllowance = 12,
            IsActive = true
        };
        var dto = new UpdateLeaveTypeDto
        {
            Name = "Casual Leave",
            IsPaid = true,
            AnnualAllowance = 15,
            Description = "Updated desc",
            IsActive = true
        };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType lt) => lt);

        var result = await _service.UpdateAsync(1, dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Casual Leave"));
        Assert.That(result.AnnualAllowance, Is.EqualTo(15));
        Assert.That(result.Description, Is.EqualTo("Updated desc"));
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<LeaveType>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((LeaveType?)null);

        var result = await _service.UpdateAsync(999, new UpdateLeaveTypeDto
        {
            Name = "X",
            IsPaid = true,
            AnnualAllowance = 0,
            IsActive = true
        });

        Assert.That(result, Is.Null);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<LeaveType>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_CanTogglePaidToUnpaid()
    {
        var existing = new LeaveType { LeaveTypeId = 2, Name = "Sick", IsPaid = true, AnnualAllowance = 8, IsActive = true };
        var dto = new UpdateLeaveTypeDto { Name = "Sick", IsPaid = false, AnnualAllowance = 8, IsActive = true };

        _mockRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType lt) => lt);

        var result = await _service.UpdateAsync(2, dto);

        Assert.That(result!.IsPaid, Is.False);
    }

    // ── DeactivateAsync ───────────────────────────────────────
    [Test]
    public async Task DeactivateAsync_WhenExists_SetsIsActiveFalse()
    {
        var lt = new LeaveType { LeaveTypeId = 1, Name = "Casual", IsPaid = true, AnnualAllowance = 12, IsActive = true };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lt);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType x) => x);

        var result = await _service.DeactivateAsync(1);

        Assert.That(result, Is.True);
        _mockRepo.Verify(r => r.UpdateAsync(It.Is<LeaveType>(x => x.IsActive == false)), Times.Once);
    }

    [Test]
    public async Task DeactivateAsync_WhenNotFound_ReturnsFalse()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((LeaveType?)null);

        var result = await _service.DeactivateAsync(999);

        Assert.That(result, Is.False);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<LeaveType>()), Times.Never);
    }

    // ── IsPaid TestCase matrix ────────────────────────────────
    [TestCase("Casual", true, 12)]
    [TestCase("Sick", true, 8)]
    [TestCase("Earned", true, 15)]
    [TestCase("Maternity", true, 90)]
    [TestCase("Marriage Leave", true, 5)]
    [TestCase("Bereavement Leave", true, 3)]
    [TestCase("LOP", false, 0)]
    public async Task CreateAsync_VariousTypes_StoresIsPaidCorrectly(
        string name, bool isPaid, int allowance)
    {
        var dto = new CreateLeaveTypeDto { Name = name, IsPaid = isPaid, AnnualAllowance = allowance, IsActive = true };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<LeaveType>()))
                 .ReturnsAsync((LeaveType lt) => lt);

        var result = await _service.CreateAsync(dto);

        Assert.That(result.IsPaid, Is.EqualTo(isPaid));
        Assert.That(result.AnnualAllowance, Is.EqualTo(allowance));
        Assert.That(result.Name, Is.EqualTo(name));
    }
}