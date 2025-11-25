using System.Diagnostics.CodeAnalysis;
using Gateway.Middleware;
using Gateway.Configuration;
using Shared.Common.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.Extensions;

/// <remarks>
/// Excluded from code coverage: Extension methods for application configuration.
/// Tested via integration tests.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Extension methods for application configuration. Tested via integration tests.")]
public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        var frontendOptions = app.Services.GetRequiredService<IOptions<FrontendOptions>>().Value;

        app.UseCors(policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin) || origin == "null")
                    return true; 
                return origin == frontendOptions.Url;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
        
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<LoginRateLimitMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<CookieAuthenticationMiddleware>();
        app.UseMiddleware<TokenForwardingMiddleware>();

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapReverseProxy();
        
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/readyz");
        app.MapPrometheusScrapingEndpoint("/metrics");

        return app;
    }

    public static async Task<WebApplication> ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await app.Services.ApplyMigrationsAsync();
        return app;
    }
}

