using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Notification.Infrastructure.Hubs;
using Notification.Infrastructure.Services;
using Shared.Common.Configuration;
using Xunit;

namespace Notification.UnitTests;

[Trait("Category", "Unit")]
public class NotificationServiceTests
{
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly Mock<IKeycloakUserService> _keycloakUserServiceMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _notificationService;
    private readonly SmtpOptions _smtpOptions;

    public NotificationServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _keycloakUserServiceMock = new Mock<IKeycloakUserService>();
        _loggerMock = new Mock<ILogger<NotificationService>>();

        _smtpOptions = new SmtpOptions
        {
            Host = "localhost",
            Port = 1025,
            FromEmail = "test@test.com",
            FromName = "NavPlat Notifications",
            EnableSsl = false
        };

        var smtpOptionsWrapper = Options.Create(_smtpOptions);

        _notificationService = new NotificationService(
            _hubContextMock.Object,
            smtpOptionsWrapper,
            _keycloakUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task TrySendSignalRNotificationAsync_ShouldReturnTrue_WhenSignalRSucceeds()
    {
        var userId = "user-123";
        var connectionId = "connection-123";
        var notification = new { Type = "Test", Message = "Test message" };
        var mockClients = new Mock<IHubClients>();
        var mockUserClients = new Mock<IClientProxy>();
        
        NotificationService.AddConnection(userId, connectionId);
        
        mockClients.Setup(c => c.User(userId)).Returns(mockUserClients.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
        mockUserClients
            .Setup(c => c.SendCoreAsync("ReceiveNotification", It.Is<object[]>(args => args != null && args.Length == 1), default))
            .Returns(Task.CompletedTask);

        var result = await _notificationService.TrySendSignalRNotificationAsync(userId, notification);

        result.Should().BeTrue();
        mockUserClients.Verify(
            c => c.SendCoreAsync("ReceiveNotification", It.Is<object[]>(args => args != null && args.Length == 1), default),
            Times.Once);
        
        // Cleanup
        NotificationService.RemoveConnection(userId, connectionId);
    }

    [Fact]
    public async Task TrySendSignalRNotificationAsync_ShouldReturnFalse_WhenUserNotConnected()
    {
        var userId = "user-123";
        var notification = new { Type = "Test", Message = "Test message" };

        // User is not registered as connected
        var result = await _notificationService.TrySendSignalRNotificationAsync(userId, notification);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TrySendSignalRNotificationAsync_ShouldReturnFalse_WhenSignalRFails()
    {
        var userId = "user-123";
        var connectionId = "connection-123";
        var notification = new { Type = "Test", Message = "Test message" };
        var mockClients = new Mock<IHubClients>();
        var mockUserClients = new Mock<IClientProxy>();
        
        // Register user as connected first
        NotificationService.AddConnection(userId, connectionId);
        
        mockClients.Setup(c => c.User(userId)).Returns(mockUserClients.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
        mockUserClients
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .ThrowsAsync(new Exception("SignalR connection failed"));

        var result = await _notificationService.TrySendSignalRNotificationAsync(userId, notification);

        result.Should().BeFalse();
        
        // Cleanup
        NotificationService.RemoveConnection(userId, connectionId);
    }

    [Fact]
    public async Task SendEmailNotificationAsync_ShouldNotSendEmail_WhenUserEmailNotFound()
    {
        var userId = "user-123";
        _keycloakUserServiceMock
            .Setup(s => s.GetUserEmailAsync(userId, default))
            .ReturnsAsync((string?)null);

        await _notificationService.Invoking(s => 
            s.SendEmailNotificationAsync(userId, "TestType", "Subject", "Body"))
            .Should().NotThrowAsync();

        _keycloakUserServiceMock.Verify(
            s => s.GetUserEmailAsync(userId, default),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailNotificationAsync_ShouldNotThrow_WhenEmailSent()
    {
        var userId = "user-123";
        var userEmail = "user@example.com";
        
        _keycloakUserServiceMock
            .Setup(s => s.GetUserEmailAsync(userId, default))
            .ReturnsAsync(userEmail);

        await _notificationService.Invoking(s => 
            s.SendEmailNotificationAsync(userId, "TestType", "Subject", "Body"))
            .Should().NotThrowAsync();

        _keycloakUserServiceMock.Verify(
            s => s.GetUserEmailAsync(userId, default),
            Times.Once);
    }
}
