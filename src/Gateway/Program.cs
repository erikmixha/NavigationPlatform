using System.Diagnostics.CodeAnalysis;
using Gateway.Extensions;
using Serilog;
using Serilog.Formatting.Compact;
using Shared.Common.Configuration;
using AuthenticationOptions = Shared.Common.Configuration.AuthenticationOptions;

// Excluded from code coverage: Application bootstrap/startup code.
// Configuration and service registration are tested via integration tests.
[assembly: ExcludeFromCodeCoverage(Justification = "Application startup/bootstrap code. Tested via integration tests.")]

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Gateway")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.FrontendOptions>(builder.Configuration.GetSection(Gateway.Configuration.FrontendOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection(RateLimitSettings.SectionName));

builder.Services.AddControllers();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddGatewayServices(builder.Configuration);
builder.Services.AddReverseProxy(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddHealthCheckServices();
builder.Services.AddRateLimiting(builder.Configuration);

var app = builder.Build();

app.ConfigureMiddleware();
app.MapEndpoints();

await app.ApplyDatabaseMigrationsAsync();

app.Run();

public partial class Program { }

