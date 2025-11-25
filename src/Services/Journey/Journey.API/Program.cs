using System.Diagnostics.CodeAnalysis;
using Journey.API.Extensions;
using Journey.Application.Extensions;
using Journey.Infrastructure.Extensions;
using Serilog;
using Serilog.Formatting.Compact;
using Shared.Common.Configuration;

// Excluded from code coverage: Application bootstrap/startup code.
// Configuration and service registration are tested via integration tests.
[assembly: ExcludeFromCodeCoverage(Justification = "Application startup/bootstrap code. Tested via integration tests.")]

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "JourneyService")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));
builder.Services.Configure<PaginationSettings>(builder.Configuration.GetSection(PaginationSettings.SectionName));
builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection(OutboxSettings.SectionName));

builder.Services.AddApiFilters();
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddSwaggerServices();
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);
builder.Services.AddMediatRPipeline();

var app = builder.Build();

app.ConfigureMiddleware();
app.MapEndpoints();

await app.ApplyDatabaseMigrationsAsync();

app.Run();

public partial class Program { }
