namespace TC.CloudGames.Payments.Api.Extensions
{
    internal static class MetricsAuthenticationExtensions
    {
        public static IApplicationBuilder UseMetricsAuthentication(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/metrics")
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ") == true)
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        var expectedToken = Environment.GetEnvironmentVariable("GRAFANA_PROMETHEUS_TOKEN");

                        if (token == expectedToken)
                        {
                            await next().ConfigureAwait(false);
                            return;
                        }
                    }

                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized").ConfigureAwait(false);
                    return;
                }

                await next().ConfigureAwait(false);
            });
        }
    }
}
