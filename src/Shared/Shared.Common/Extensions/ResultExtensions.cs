using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Shared.Common.Models;

namespace Shared.Common.Extensions;

/// <summary>
/// Extension methods for converting Result types to HTTP responses.
/// </summary>
/// <remarks>
/// Excluded from code coverage: Extension methods for result conversion.
/// Result mapping is tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Extension methods for result conversion. Tested via integration tests.")]
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an IActionResult with appropriate HTTP status code.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="defaultStatusCode">The default status code to use if no specific mapping exists (default: 400).</param>
    /// <returns>An IActionResult representing the problem details response.</returns>
    public static IActionResult ToProblemDetails(
        this Result.Result result,
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

    /// <summary>
    /// Converts a Result&lt;TValue&gt; to an IActionResult with appropriate HTTP status code.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="defaultStatusCode">The default status code to use if no specific mapping exists (default: 400).</param>
    /// <returns>An IActionResult representing the problem details response.</returns>
    public static IActionResult ToProblemDetails<T>(
        this Result.Result<T> result,
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
            var code when code.Contains("PublicLinkRevoked") => 410,
            _ => defaultStatusCode
        };
    }
}
