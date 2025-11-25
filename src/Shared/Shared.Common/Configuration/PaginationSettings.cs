using System.Diagnostics.CodeAnalysis;

namespace Shared.Common.Configuration;

/// <remarks>
/// Excluded from code coverage: Configuration class with simple properties.
/// Configuration is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration class. Tested via integration tests.")]
public sealed class PaginationSettings
{
    public const string SectionName = "Pagination";

    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
}

