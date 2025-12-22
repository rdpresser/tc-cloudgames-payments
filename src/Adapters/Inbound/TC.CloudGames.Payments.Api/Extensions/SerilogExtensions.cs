namespace TC.CloudGames.Payments.Api.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class SerilogExtensions
    {
        public static IHostBuilder UseCustomSerilog(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            return hostBuilder.UseSerilog((hostContext, services, loggerConfiguration) =>
            {
                // Read default configuration (sinks, levels, overrides)
                loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration);

                // Useful values
                var environment = configuration["ASPNETCORE_ENVIRONMENT"]?.ToLower() ?? "development";
                var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? TelemetryConstants.Version;
                var instanceId = Environment.MachineName;

                // Timezone (if configured)
                var timeZoneId = configuration["TimeZone"] ?? "UTC";
                TimeZoneInfo? timeZone = null;
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                }
                catch
                {
                    // Fallback to UTC if TimeZone is invalid
                    timeZone = TimeZoneInfo.Utc;
                }

                // --- Essential Enrichers ---
                // Maintains enrichment with span/trace, context and static properties
                loggerConfiguration.Enrich.With(new UtcToLocalTimeEnricher(timeZone));
                loggerConfiguration.Enrich.WithSpan();           // Ensures trace_id/span_id when available
                loggerConfiguration.Enrich.FromLogContext();

                // OpenTelemetry semantic conventions / resource consistency
                loggerConfiguration.Enrich.WithProperty("service.name", TelemetryConstants.ServiceName);
                loggerConfiguration.Enrich.WithProperty("service.namespace", TelemetryConstants.ServiceNamespace);
                loggerConfiguration.Enrich.WithProperty("service.version", serviceVersion);
                loggerConfiguration.Enrich.WithProperty("deployment.environment", environment);
                loggerConfiguration.Enrich.WithProperty("cloud.provider", "azure");
                loggerConfiguration.Enrich.WithProperty("cloud.platform", "azure_kubernetes_service");
                loggerConfiguration.Enrich.WithProperty("service.instance.id", instanceId);

                // NOTE: Console sink (JSON stdout) is already configured in appsettings.json via ReadFrom.Configuration()
                // No need to add WriteTo.Console() here to avoid duplicate log entries
                // Grafana Agent / Loki collects from stdout as configured in appsettings.Development.json and appsettings.Production.json
            });
        }
    }
}
