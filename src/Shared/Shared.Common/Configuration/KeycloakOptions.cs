namespace Shared.Common.Configuration;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string AdminClientId { get; set; } = string.Empty;
    public string AdminClientSecret { get; set; } = string.Empty;
}

