namespace TC.CloudGames.Payments.Api.Extensions
{
    /// <summary>
    /// Captures telemetry exporter configuration information for logging.
    /// Used to defer logging until ILogger is available in Program.cs.
    /// </summary>
    internal class TelemetryExporterInfo
    {
        /// <summary>
        /// Type of exporter: "AzureMonitor", "OTLP", or "None"
        /// </summary>
        public string ExporterType { get; set; } = "None";

        /// <summary>
        /// Sampling ratio for Azure Monitor (0.0 to 1.0)
        /// </summary>
        public float? SamplingRatio { get; set; }

        /// <summary>
        /// Endpoint URL for OTLP exporter
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// Protocol for OTLP exporter (grpc or http/protobuf)
        /// </summary>
        public string? Protocol { get; set; }
    }
}
