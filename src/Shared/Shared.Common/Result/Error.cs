using System.Diagnostics.CodeAnalysis;

namespace Shared.Common.Result;

/// <summary>
/// Represents an error that occurred during an operation.
/// </summary>
/// <param name="Code">The error code that uniquely identifies the type of error.</param>
/// <param name="Message">The human-readable error message.</param>
/// <remarks>
/// Excluded from code coverage: Simple error record with static members.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Simple error record. Tested via integration tests.")]
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error (used for successful results).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents an error when a null value was provided where a non-null value was expected.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
}
