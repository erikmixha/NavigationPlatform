namespace Shared.Common.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public bool AllowAnyOriginInDevelopment { get; set; } = true;
}

