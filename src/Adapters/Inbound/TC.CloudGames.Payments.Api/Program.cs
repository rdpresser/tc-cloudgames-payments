var builder = WebApplication.CreateBuilder(args);

// Configure environment variables (will skip if running under .NET Aspire)
builder.ConfigureEnvironmentVariables();

// Configure Serilog as logging provider
builder.Host.UseCustomSerilog(builder.Configuration);

//***************** ADICIONAR **************************************************/
//builder.AddCustomLoggingTelemetry()
//********************************************************************************/

// Register application, infrastructure and API services
builder.Services.AddPaymentServices(builder);
////builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (!builder.Environment.IsEnvironment("Testing"))
{
    await app.CreateMessageDatabase().ConfigureAwait(false);
}

// Use metrics authentication middleware extension
app.UseMetricsAuthentication();

app.UseCustomMiddlewares();

// Run the application
await app.RunAsync().ConfigureAwait(false);