using System.Diagnostics.CodeAnalysis;

namespace Shared.Common.Configuration;

/// <remarks>
/// Excluded from code coverage: Configuration class with simple properties.
/// Configuration is tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Configuration class. Tested via integration tests.")]
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Authority { get; set; } = string.Empty;
    public string PublicAuthority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
}

