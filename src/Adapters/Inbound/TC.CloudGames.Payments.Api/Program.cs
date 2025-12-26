var builder = WebApplication.CreateBuilder(args);

// Configure environment variables (will skip if running under .NET Aspire)
builder.ConfigureEnvironmentVariables();

// Configure Serilog as logging provider
builder.Host.UseCustomSerilog(builder.Configuration);

// Register application, infrastructure and API services
builder.Services.AddPaymentServices(builder);
builder.Services.AddInfrastructure();

var app = builder.Build();

if (!builder.Environment.IsEnvironment("Testing"))
{
    await app.CreateMessageDatabase().ConfigureAwait(false);
}

// Get logger instance for Program and log telemetry configuration
var logger = app.Services.GetRequiredService<ILogger<TC.CloudGames.Payments.Api.Program>>();
TelemetryConstants.LogTelemetryConfiguration(logger, app.Configuration);

// Log APM/exporter configuration (Azure Monitor, OTLP, etc.)
// This info was populated during service configuration in ServiceCollectionExtensions
var exporterInfo = app.Services.GetService<TC.CloudGames.Payments.Api.Extensions.TelemetryExporterInfo>();
TelemetryConstants.LogApmExporterConfiguration(logger, exporterInfo);

// Use metrics authentication middleware extension
app.UseMetricsAuthentication();

app.UseCustomMiddlewares();

// Run the application
await app.RunAsync().ConfigureAwait(false);