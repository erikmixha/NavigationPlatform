using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.DTOs;
using Notification.Infrastructure.Persistence;
using Xunit;

namespace Notification.IntegrationTests;

[Trait("Category", "Integration")]
public class NotificationApiIntegrationTests : IClassFixture<NotificationApiTestFixture>, IAsyncLifetime
{
    private readonly NotificationApiTestFixture _fixture;

    public NotificationApiIntegrationTests(NotificationApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Notifications_ShouldReturn200_WithEmptyList_WhenNoNotifications()
    {
        var client = _fixture.CreateClientWithUser("empty-test-user-id");

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifications.Should().NotBeNull();
        notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_Notifications_ShouldReturnOnlyUserNotifications()
    {
        var user1Client = _fixture.CreateClientWithUser("user-1");
        var user2Client = _fixture.CreateClientWithUser("user-2");

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification1 = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            "user-1",
            "TestType",
            "Test Title 1",
            "Test Message 1");
        var notification2 = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            "user-2",
            "TestType",
            "Test Title 2",
            "Test Message 2");

        await context.Notifications.AddRangeAsync(notification1, notification2);
        await context.SaveChangesAsync();

        var response1 = await user1Client.GetAsync("/api/notifications");
        var response2 = await user2Client.GetAsync("/api/notifications");

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var notifications1 = await response1.Content.ReadFromJsonAsync<List<NotificationDto>>();
        var notifications2 = await response2.Content.ReadFromJsonAsync<List<NotificationDto>>();

        notifications1.Should().HaveCount(1);
        notifications1![0].Title.Should().Be("Test Title 1");
        notifications2.Should().HaveCount(1);
        notifications2![0].Title.Should().Be("Test Title 2");
    }

    [Fact]
    public async Task GET_Notifications_WithIsReadFilter_ShouldReturnOnlyUnreadNotifications()
    {
        var userId = Guid.NewGuid().ToString();
        var client = _fixture.CreateClientWithUser(userId);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var unreadNotification = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            userId,
            "TestType",
            "Unread Title",
            "Unread Message");
        var readNotification = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            userId,
            "TestType",
            "Read Title",
            "Read Message");
        readNotification.MarkAsRead();

        await context.Notifications.AddRangeAsync(unreadNotification, readNotification);
        await context.SaveChangesAsync();

        var response = await client.GetAsync("/api/notifications?isRead=false");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifications.Should().HaveCount(1);
        notifications![0].IsRead.Should().BeFalse();
        notifications[0].Title.Should().Be("Unread Title");
    }

    [Fact]
    public async Task GET_Notifications_UnreadCount_ShouldReturnCorrectCount()
    {
        var userId = "unread-count-test-user-id";
        var client = _fixture.CreateClientWithUser(userId);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification1 = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            userId,
            "TestType",
            "Title 1",
            "Message 1");
        var notification2 = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            userId,
            "TestType",
            "Title 2",
            "Message 2");
        var readNotification = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            userId,
            "TestType",
            "Read Title",
            "Read Message");
        readNotification.MarkAsRead();

        await context.Notifications.AddRangeAsync(notification1, notification2, readNotification);
        await context.SaveChangesAsync();

        var response = await client.GetAsync("/api/notifications/unread/count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        result.Should().NotBeNull();
        result!["count"].Should().Be(2);
    }

    [Fact]
    public async Task POST_Notifications_MarkAsRead_ShouldMarkNotificationAsRead()
    {
        var client = _fixture.CreateClientWithUser("test-user-id");

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            "test-user-id",
            "TestType",
            "Test Title",
            "Test Message");

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var response = await client.PostAsync($"/api/notifications/{notification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        context.Entry(notification).Reload();
        notification.Should().NotBeNull();
        notification.IsRead.Should().BeTrue();
        notification.ReadOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Notifications_MarkAsRead_ShouldReturn404_WhenNotificationNotFound()
    {
        var client = _fixture.CreateClientWithUser("test-user-id");
        var nonExistentId = Guid.NewGuid();

        var response = await client.PostAsync($"/api/notifications/{nonExistentId}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Notifications_MarkAsRead_ShouldReturn403_WhenNotOwner()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var otherUserClient = _fixture.CreateClientWithUser("other-user-id");

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            "owner-user-id",
            "TestType",
            "Test Title",
            "Test Message");

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var response = await otherUserClient.PostAsync($"/api/notifications/{notification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_Notifications_MarkAsUnread_ShouldMarkNotificationAsUnread()
    {
        var client = _fixture.CreateClientWithUser("test-user-id");

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = new Notification.Domain.Entities.Notification(
            Guid.NewGuid(),
            "test-user-id",
            "TestType",
            "Test Title",
            "Test Message");
        notification.MarkAsRead();

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var response = await client.PostAsync($"/api/notifications/{notification.Id}/unread", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        context.Entry(notification).Reload();
        notification.Should().NotBeNull();
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GET_Notifications_ShouldReturn401_WhenUnauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Clear();

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

