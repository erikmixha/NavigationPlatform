using System.Diagnostics.CodeAnalysis;
using Notification.API.Extensions;
using Notification.Application.Extensions;
using Notification.Infrastructure.Extensions;
using Serilog;
using Serilog.Formatting.Compact;
using Shared.Common.Configuration;

// Excluded from code coverage: Application bootstrap/startup code.
// Configuration and service registration are tested via integration tests.
[assembly: ExcludeFromCodeCoverage(Justification = "Application startup/bootstrap code. Tested via integration tests.")]

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Notification.API")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddSignalRServices();
builder.Services.AddMessagingServices(builder.Configuration);
builder.Services.AddCorsServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddHealthCheckServices();

var app = builder.Build();

await app.Services.ApplyMigrationsAsync();

app.ConfigureMiddleware();
app.MapEndpoints();

Log.Information("Notification API started");

app.Run();

public partial class Program { }
