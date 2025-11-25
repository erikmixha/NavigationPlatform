using System.Diagnostics.CodeAnalysis;
using Reward.Worker.Extensions;
using Serilog;
using Serilog.Formatting.Compact;
using Shared.Common.Configuration;

// Excluded from code coverage: Application bootstrap/startup code.
// Configuration and service registration are tested via integration tests.
[assembly: ExcludeFromCodeCoverage(Justification = "Application startup/bootstrap code. Tested via integration tests.")]

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Reward.Worker")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<RewardSettings>(builder.Configuration.GetSection(RewardSettings.SectionName));
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddMessagingServices(builder.Configuration);

var host = builder.Build();

await host.ApplyDatabaseMigrationsAsync();

Log.Information("Reward Worker started");

await host.RunAsync();

Log.Information("Reward Worker stopped");
Log.CloseAndFlush();
