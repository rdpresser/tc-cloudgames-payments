namespace TC.CloudGames.Payments.Api.Telemetry
{
    internal static class TelemetryConstants
    {
        // Versions
        public const string Version = "1.0.0";

        // Service Identity - Centralized for consistency (matches Docker Compose)
        public const string ServiceName = "tccloudgames-payments";
        public const string ServiceNamespace = "tccloudgames";

        // Meter Names for OpenTelemetry Metrics
        public const string PaymentsMeterName = "TC.CloudGames.Payments.Api.Metrics";

        // Activity Source Names for OpenTelemetry Tracing
        public const string PaymentsActivitySource = "TC.CloudGames.Payments.Api";
        public const string DatabaseActivitySource = "TC.CloudGames.Payments.Api.Database";
        public const string CacheActivitySource = "TC.CloudGames.Payments.Api.Cache";

        // Header Names (standardized)
        public const string CorrelationIdHeader = "x-correlation-id";

        // Tag Names (using underscores for consistency with Loki labels)
        public const string ServiceComponent = "service.component";
        public const string UserId = "user_id";
        public const string UserAction = "user_action";
        public const string SessionId = "session_id";
        public const string ErrorType = "error_type";

        // Default Values
        public const string AnonymousUser = "anonymous";

        // Service Components
        public const string UserComponent = "user";
        public const string DatabaseComponent = "database";
        public const string CacheComponent = "cache";

        /// <summary>
        /// Logs telemetry configuration details using Microsoft.Extensions.Logging.ILogger
        /// </summary>
        public static void LogTelemetryConfiguration(Microsoft.Extensions.Logging.ILogger logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            logger.LogInformation("=== TELEMETRY DEBUG INFO ===");
            logger.LogInformation("Service Name: {ServiceName}", ServiceName);
            logger.LogInformation("Service Namespace: {ServiceNamespace}", ServiceNamespace);
            logger.LogInformation("Telemetry Version: {Version}", Version);
            logger.LogInformation("Correlation Header: {CorrelationIdHeader}", CorrelationIdHeader);
            logger.LogInformation("Payments Meter: {PaymentsMeterName}", PaymentsMeterName);
            logger.LogInformation("Payments Activity Source: {PaymentsActivitySource}", PaymentsActivitySource);
            logger.LogInformation("Database Activity Source: {DatabaseActivitySource}", DatabaseActivitySource);
            logger.LogInformation("Cache Activity Source: {CacheActivitySource}", CacheActivitySource);
            logger.LogInformation("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NOT SET");
            logger.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
            logger.LogInformation("Container Name: {ContainerName}", Environment.GetEnvironmentVariable("HOSTNAME") ?? "NOT SET");

            // Load Grafana configuration via Helper
            var grafanaSettings = TC.CloudGames.SharedKernel.Infrastructure.Telemetry.GrafanaHelper.Build(configuration);

            logger.LogInformation("============================");
            logger.LogInformation("=== GRAFANA AGENT CONFIG ===");

            // Agent Status (CRITICAL INFO)
            if (grafanaSettings.Agent.Enabled)
            {
                logger.LogInformation("? Grafana Agent: ENABLED");
                logger.LogInformation("   ? OTLP Export: ACTIVE");
                logger.LogInformation("   ? Traces will be sent to Grafana Agent");
                logger.LogInformation("   ? Logs: stdout ? Agent ? Grafana Cloud Loki");
                logger.LogInformation("   ? Metrics: /metrics ? Agent scrape ? Grafana Cloud Prometheus");
            }
            else
            {
                logger.LogWarning("??  Grafana Agent: DISABLED");
                logger.LogWarning("   ? OTLP Export: INACTIVE");
                logger.LogWarning("   ? Traces will be generated but NOT exported");
                logger.LogWarning("   ? Logs: stdout only (not sent to Grafana Cloud)");
                logger.LogWarning("   ? Metrics: /metrics endpoint available (not scraped)");
                logger.LogWarning("   ? To enable: Set Grafana:Agent:Enabled=true or GRAFANA_AGENT_ENABLED=true");
            }

            logger.LogInformation("Agent Host: {AgentHost}", grafanaSettings.Agent.Host);
            logger.LogInformation("Agent OTLP gRPC Port: {OtlpGrpcPort}", grafanaSettings.Agent.OtlpGrpcPort);
            logger.LogInformation("Agent OTLP HTTP Port: {OtlpHttpPort}", grafanaSettings.Agent.OtlpHttpPort);
            logger.LogInformation("Agent Metrics Port: {MetricsPort}", grafanaSettings.Agent.MetricsPort);
            logger.LogInformation("OTLP Endpoint: {OtlpEndpoint}", grafanaSettings.Otlp.Endpoint);
            logger.LogInformation("OTLP Protocol: {OtlpProtocol}", grafanaSettings.Otlp.Protocol);
            logger.LogInformation("OTLP Headers: {OtlpHeaders}",
                string.IsNullOrWhiteSpace(grafanaSettings.Otlp.Headers) ? "NOT SET" : "***CONFIGURED***");
            logger.LogInformation("OTLP Timeout: {OtlpTimeout}s", grafanaSettings.Otlp.TimeoutSeconds);
            logger.LogInformation("OTLP Insecure: {OtlpInsecure}", grafanaSettings.Otlp.Insecure);
            logger.LogInformation("============================");
        }

        /// <summary>
        /// Logs APM/Telemetry exporter configuration details.
        /// This should be called from Program.cs after the logger is fully configured.
        /// </summary>
        public static void LogApmExporterConfiguration(Microsoft.Extensions.Logging.ILogger logger, TC.CloudGames.Payments.Api.Extensions.TelemetryExporterInfo? exporterInfo)
        {
            if (exporterInfo == null)
            {
                logger.LogWarning("Telemetry exporter information not available");
                return;
            }

            logger.LogInformation("====================================================================================");

            switch (exporterInfo.ExporterType.ToUpperInvariant())
            {
                case "AZUREMONITOR":
                    logger.LogInformation("Azure Monitor configured - Telemetry will be exported to Application Insights");
                    logger.LogInformation("Using DefaultAzureCredential for RBAC/Workload Identity authentication");
                    if (exporterInfo.SamplingRatio.HasValue)
                    {
                        logger.LogInformation("Sampling Ratio: {SamplingRatio:P0}", exporterInfo.SamplingRatio.Value);
                    }
                    logger.LogInformation("Live Metrics: Enabled");
                    break;

                case "OTLP":
                    logger.LogInformation("OTLP Exporter configured - Endpoint: {Endpoint}, Protocol: {Protocol}",
                        exporterInfo.Endpoint ?? "NOT SET",
                        exporterInfo.Protocol ?? "NOT SET");
                    break;

                case "NONE":
                    logger.LogWarning("No APM exporter configured - Telemetry will be generated but NOT exported.");
                    logger.LogWarning("To enable Azure Monitor: Set APPLICATIONINSIGHTS_CONNECTION_STRING");
                    logger.LogWarning("To enable Grafana: Set GRAFANA_AGENT_ENABLED=true and OTEL_EXPORTER_OTLP_ENDPOINT");
                    break;

                default:
                    logger.LogWarning("Unknown telemetry exporter type: {ExporterType}", exporterInfo.ExporterType);
                    break;
            }

            logger.LogInformation("====================================================================================");
        }
    }
}
