namespace Notification.Application.DTOs;

public sealed record NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReadOnUtc { get; init; }
}

