using Notification.API.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Result;
using System.Security.Claims;

namespace Notification.API.Extensions;

/// <remarks>
/// Excluded from code coverage: Extension methods for result conversion.
/// Result mapping is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for result conversion. Tested via integration tests.")]
public static class ResultExtensions
{
    public static IActionResult ToProblemDetails(
        this Result result,
        HttpContext httpContext,
        int defaultStatusCode = 400)
    {
        var statusCode = MapErrorToStatusCode(result.Error.Code, defaultStatusCode);
        
        var problemDetails = ProblemDetailsResponse.CreateFromError(
            result.Error.Code,
            result.Error.Message,
            statusCode,
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    public static IActionResult ToProblemDetails<T>(
        this Result<T> result,
        HttpContext httpContext,
        int defaultStatusCode = 400)
    {
        var statusCode = MapErrorToStatusCode(result.Error.Code, defaultStatusCode);
        
        var problemDetails = ProblemDetailsResponse.CreateFromError(
            result.Error.Code,
            result.Error.Message,
            statusCode,
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private static int MapErrorToStatusCode(string errorCode, int defaultStatusCode)
    {
        return errorCode switch
        {
            var code when code.EndsWith(".NotFound") => 404,
            var code when code.EndsWith(".Forbidden") => 403,
            var code when code.EndsWith(".Unauthorized") => 401,
            var code when code.EndsWith(".Conflict") => 409,
            _ => defaultStatusCode
        };
    }
}

public static class ControllerBaseExtensions
{
    public static string GetUserId(this ControllerBase controller)
    {
        return controller.User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? controller.User.FindFirstValue("sub") 
            ?? throw new UnauthorizedAccessException("User ID not found");
    }
}

