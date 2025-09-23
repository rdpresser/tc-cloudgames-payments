using TC.CloudGames.Payments.Api.Middleware;

namespace TC.CloudGames.Payments.Api.Extensions
{
    [ExcludeFromCodeCoverage]
    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            return app;
        }

        // Configures custom middlewares including HTTPS redirection, exception handling, correlation, logging, and health checks
        public static IApplicationBuilder UseCustomMiddlewares(this IApplicationBuilder app)
        {
            app.UseHttpsRedirection()
                .UseCustomExceptionHandler()
                .UseCorrelationMiddleware()

                //***************** ADICIONAR **************************************************/
                //.UseMiddleware<TelemetryMiddleware>() // Add telemetry middleware after correlation
                //******************************************************************************/

                .UseSerilogRequestLogging()
                .UseHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .UseHealthChecks("/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .UseHealthChecks("/live", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("live"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            // Add Prometheus metrics endpoint
            //***************** ADICIONAR **************************************************/
            //.UseOpenTelemetryPrometheusScrapingEndpoint("/metrics")
            //******************************************************************************/

            return app;
        }

        public async static Task<IApplicationBuilder> CreateMessageDatabase(this IApplicationBuilder app)
        {
            // Ensure outbox database exists before Wolverine is used
            var connProvider = app.ApplicationServices.GetRequiredService<IConnectionStringProvider>();
            await PostgresDatabaseHelper.EnsureDatabaseExists(connProvider);

            return app;
        }
    }
}
