using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Common.Middleware;

/// <summary>
/// Middleware that adds correlation ID tracking to requests for distributed tracing.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Middleware for correlation ID tracking.
/// Middleware flow is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Middleware - correlation ID tracking tested via integration tests.")]
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
