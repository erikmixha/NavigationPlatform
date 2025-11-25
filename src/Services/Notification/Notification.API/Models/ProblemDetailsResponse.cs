using System.Text.Json.Serialization;

namespace Notification.API.Models;

public sealed class ProblemDetailsResponse
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "about:blank";

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; init; }

    public static ProblemDetailsResponse CreateFromError(
        string errorCode,
        string errorMessage,
        int statusCode,
        string? traceId = null,
        string? instance = null)
    {
        var typeMap = new Dictionary<string, string>
        {
            { "Notification.NotFound", "https://tools.ietf.org/html/rfc7231#section-6.5.4" },
            { "Notification.Forbidden", "https://tools.ietf.org/html/rfc7231#section-6.5.3" }
        };

        var type = typeMap.TryGetValue(errorCode, out var t)
            ? t
            : "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        return new ProblemDetailsResponse
        {
            Type = type,
            Title = GetTitleFromStatusCode(statusCode),
            Status = statusCode,
            Detail = errorMessage,
            Instance = instance,
            TraceId = traceId,
            Errors = new Dictionary<string, string[]>
            {
                { errorCode, new[] { errorMessage } }
            }
        };
    }

    private static string GetTitleFromStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        500 => "Internal Server Error",
        _ => "An error occurred"
    };
}

