using Moq;
using NUnit.Framework;
using EasyPay.Core.Entities;
using EasyPay.Core.Interfaces.Repositories;
using EasyPay.Core.Interfaces.Services;
using EasyPay.Infrastructure.Services;

namespace EasyPay.Tests.Services;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<INotificationRepository> _mockRepo;
    private INotificationService          _service;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<INotificationRepository>();
        _service  = new NotificationService(_mockRepo.Object);
    }

    // ── GetForUserAsync ───────────────────────────────────────
    [Test]
    public async Task GetForUserAsync_ReturnsMappedDtos()
    {
        var notifications = new List<Notification>
        {
            new() { NotificationId=1, UserId=5, Title="Payroll Generated",
                    Message="Your salary for 6/2026 is ready.", IsRead=false, CreatedDate=DateTime.Now },
            new() { NotificationId=2, UserId=5, Title="Leave Approved",
                    Message="Your casual leave is approved.", IsRead=true, CreatedDate=DateTime.Now.AddDays(-1) }
        };
        _mockRepo.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(notifications);

        var result = (await _service.GetForUserAsync(5)).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Title, Is.EqualTo("Payroll Generated"));
        Assert.That(result[0].IsRead, Is.False);
        Assert.That(result[1].IsRead, Is.True);
        _mockRepo.Verify(r => r.GetByUserIdAsync(5), Times.Once);
    }

    [Test]
    public async Task GetForUserAsync_WhenNoNotifications_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync(new List<Notification>());

        var result = await _service.GetForUserAsync(99);

        Assert.That(result, Is.Empty);
    }

    // ── GetUnreadCountAsync ───────────────────────────────────
    [Test]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        _mockRepo.Setup(r => r.GetUnreadCountAsync(5)).ReturnsAsync(3);

        var result = await _service.GetUnreadCountAsync(5);

        Assert.That(result.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetUnreadCountAsync_WhenAllRead_ReturnsZero()
    {
        _mockRepo.Setup(r => r.GetUnreadCountAsync(5)).ReturnsAsync(0);

        var result = await _service.GetUnreadCountAsync(5);

        Assert.That(result.Count, Is.EqualTo(0));
    }

    // ── NotifyAsync ───────────────────────────────────────────
    [Test]
    public async Task NotifyAsync_CallsRepositoryCreateOnce()
    {
        _mockRepo.Setup(r => r.CreateAsync(5, "Test Title", "Test message"))
                  .Returns(Task.CompletedTask);

        await _service.NotifyAsync(5, "Test Title", "Test message");

        _mockRepo.Verify(r => r.CreateAsync(5, "Test Title", "Test message"), Times.Once);
    }

    [TestCase("Payroll Generated", "Your salary for 7/2026 is ready.")]
    [TestCase("Leave Approved",    "Your casual leave is approved.")]
    [TestCase("Leave Rejected",    "Your sick leave has been rejected.")]
    [TestCase("Benefit Assigned",  "Health Insurance assigned to your account.")]
    public async Task NotifyAsync_VariousTitles_CallsCreateCorrectly(string title, string message)
    {
        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

        await _service.NotifyAsync(1, title, message);

        _mockRepo.Verify(r => r.CreateAsync(1, title, message), Times.Once);
    }

    // ── MarkAsReadAsync ───────────────────────────────────────
    [Test]
    public async Task MarkAsReadAsync_CallsRepositoryWithCorrectArgs()
    {
        _mockRepo.Setup(r => r.MarkAsReadAsync(3, 5)).Returns(Task.CompletedTask);

        await _service.MarkAsReadAsync(3, 5);

        _mockRepo.Verify(r => r.MarkAsReadAsync(3, 5), Times.Once);
    }

    // ── MarkAllReadAsync ──────────────────────────────────────
    [Test]
    public async Task MarkAllReadAsync_CallsRepositoryForUser()
    {
        _mockRepo.Setup(r => r.MarkAllReadAsync(5)).Returns(Task.CompletedTask);

        await _service.MarkAllReadAsync(5);

        _mockRepo.Verify(r => r.MarkAllReadAsync(5), Times.Once);
    }
}
