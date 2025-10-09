namespace TC.CloudGames.Payments.Api.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPaymentServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            // Add Marten configuration only if not testing
            if (!builder.Environment.IsEnvironment("Testing"))
            {
                services.AddMartenEventSourcing();
                builder.AddWolverineMessaging();
            }

            services.AddHttpClient()
                .AddCorrelationIdGenerator()
                .AddHttpContextAccessor()
                .ConfigureAppSettings(builder.Configuration)
                .AddCustomHealthCheck()
                .AddCustomOpenTelemetry(builder.Configuration);

            return services;
        }

        public static WebApplicationBuilder AddCustomLoggingTelemetry(this WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();

            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;

                // Enhanced resource configuration for logs using centralized constants
                options.SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(TelemetryConstants.ServiceName,
                                   serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? TelemetryConstants.Version)
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = (builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development").ToLowerInvariant(),
                            ["service.namespace"] = TelemetryConstants.ServiceNamespace.ToLowerInvariant(),
                            ["cloud.provider"] = "azure",
                            ["cloud.platform"] = "azure_container_apps"
                        }));

                options.AddOtlpExporter();
            });

            return builder;
        }

        // Health Checks with Enhanced Telemetry
        public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddNpgSql(sp =>
                {
                    var connectionProvider = sp.GetRequiredService<IConnectionStringProvider>();
                    return connectionProvider.ConnectionString;
                },
                    name: "PostgreSQL",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["db", "sql", "postgres", "live", "ready"])
                .AddCheck("Memory", () =>
                {
                    var allocated = GC.GetTotalMemory(false);
                    var mb = allocated / 1024 / 1024;

                    return mb < 1024
                    ? HealthCheckResult.Healthy($"Memory usage: {mb} MB")
                    : HealthCheckResult.Degraded($"High memory usage: {mb} MB");
                },
                    tags: ["memory", "system", "live"])
                .AddCheck("Custom-Metrics", () =>
                {
                    // Add any custom health logic for your metrics system
                    return HealthCheckResult.Healthy("Custom metrics are functioning");
                },
                    tags: ["metrics", "telemetry", "live"]);

            return services;
        }

        public static IServiceCollection AddCustomOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? TelemetryConstants.Version;
            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
            var instanceId = Environment.MachineName;
            var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            var otlpHeaders = configuration["OTEL_EXPORTER_OTLP_HEADERS"];

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(TelemetryConstants.ServiceName, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = environment.ToLowerInvariant(),
                        ["service.namespace"] = TelemetryConstants.ServiceNamespace.ToLowerInvariant(),
                        ["service.instance.id"] = instanceId,
                        ["container.name"] = Environment.GetEnvironmentVariable("HOSTNAME") ?? instanceId,
                        ["cloud.provider"] = "azure",
                        ["cloud.platform"] = "azure_container_apps",
                        ["service.team"] = "engineering",
                        ["service.owner"] = "devops"
                    }))
                .WithMetrics(metricsBuilder =>
                    metricsBuilder
                        // ASP.NET Core and system instrumentation
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation() // CPU, Memory, GC metrics
                        .AddNpgsqlInstrumentation()
                        .AddFusionCacheInstrumentation()
                        // Custom meters (app + Wolverine + Marten)
                        .AddMeter("System.Runtime")
                        .AddMeter("Microsoft.AspNetCore.Hosting")
                        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                        .AddMeter("System.Net.Http")
                        .AddMeter("Wolverine")
                        .AddMeter("Marten")
                        .AddMeter(TelemetryConstants.PaymentsMeterName)
                        // Exporters
                        .AddPrometheusExporter()
                        .AddOtlpExporter(opt =>
                        {
                            opt.Protocol = OtlpExportProtocol.Grpc;
                            opt.Endpoint = new Uri(otlpEndpoint ?? "");
                            opt.Headers = otlpHeaders ?? "";
                            opt.ExportProcessorType = ExportProcessorType.Batch;
                            opt.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                            {
                                MaxQueueSize = 2048,
                                ScheduledDelayMilliseconds = 5000,
                                ExporterTimeoutMilliseconds = 3000
                            };
                        }))
                .WithTracing(tracingBuilder =>
                    tracingBuilder
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.FilterHttpRequestMessage = request =>
                            {
                                // Filter out health check and metrics requests
                                var path = request.RequestUri?.AbsolutePath ?? "";
                                return !path.Contains("/health") && !path.Contains("/metrics") && !path.Contains("/prometheus");
                            };
                            options.EnrichWithHttpRequestMessage = (activity, request) =>
                            {
                                activity.SetTag("http.request.method", request.Method.ToString());
                                activity.SetTag("http.request.body.size", request.Content?.Headers?.ContentLength);
                                activity.SetTag("user_agent", request.Headers.UserAgent?.ToString());
                            };
                            options.EnrichWithHttpResponseMessage = (activity, response) =>
                            {
                                activity.SetTag("http.response.status_code", (int)response.StatusCode);
                                activity.SetTag("http.response.body.size", response.Content?.Headers?.ContentLength);
                            };
                        })
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = httpContext =>
                            {
                                // Filter out health check, metrics, and prometheus requests
                                var path = httpContext.Request.Path.Value ?? "";
                                return !path.Contains("/health") && !path.Contains("/metrics") && !path.Contains("/prometheus");
                            };
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.method", request.Method);
                                activity.SetTag("http.scheme", request.Scheme);
                                activity.SetTag("http.host", request.Host.Value);
                                activity.SetTag("http.target", request.Path);
                                if (request.ContentLength.HasValue)
                                    activity.SetTag("http.request_content_length", request.ContentLength.Value);

                                activity.SetTag("http.request.size", request.ContentLength);
                                activity.SetTag("user.id", request.HttpContext.User?.Identity?.Name);
                                activity.SetTag("user.authenticated", request.HttpContext.User?.Identity?.IsAuthenticated);
                                activity.SetTag("http.route", request.HttpContext.GetRouteValue("action")?.ToString());
                                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());

                                if (request.Headers.TryGetValue(TelemetryConstants.CorrelationIdHeader, out var correlationId))
                                {
                                    activity.SetTag("correlation.id", correlationId.FirstOrDefault());
                                }
                            };
                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.status_code", response.StatusCode);
                                if (response.ContentLength.HasValue)
                                    activity.SetTag("http.response_content_length", response.ContentLength.Value);

                                activity.SetTag("http.response.size", response.ContentLength);
                            };

                            options.EnrichWithException = (activity, exception) =>
                            {
                                activity.SetTag("exception.type", exception.GetType().Name);
                                activity.SetTag("exception.message", exception.Message);
                                activity.SetTag("exception.stacktrace", exception.StackTrace);
                            };
                        })
                        .AddNpgsql()
                        //.AddFusionCacheInstrumentation()
                        .AddRedisInstrumentation()
                        .AddSource(TelemetryConstants.DatabaseActivitySource)
                        .AddSource(TelemetryConstants.CacheActivitySource)
                        // Important: filter out internal sources that cause 503s
                        .SetSampler(new AlwaysOnSampler())
                        .AddProcessor(new FilteringActivityProcessor(activity =>
                            !activity.Source.Name.StartsWith("Azure.") &&
                            !activity.Source.Name.StartsWith("Wolverine") &&
                            !activity.Source.Name.StartsWith("Marten") &&
                            !activity.DisplayName.Contains("ServiceBus")))
                        ////.AddSource("Wolverine")
                        ////.AddSource("Marten")
                        // OTLP Exporter in batch, async, non-blocking
                        .AddOtlpExporter(opt =>
                        {
                            opt.Protocol = OtlpExportProtocol.Grpc;
                            opt.Endpoint = new Uri(otlpEndpoint ?? "");
                            opt.Headers = otlpHeaders ?? "";
                            opt.ExportProcessorType = ExportProcessorType.Batch;
                            opt.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                            {
                                MaxQueueSize = 2048,
                                ScheduledDelayMilliseconds = 5000,
                                ExporterTimeoutMilliseconds = 3000
                            };
                        }));

            // Register custom metrics classes
            services.AddSingleton<SystemMetrics>();

            return services;
        }

        // 2) Configure Wolverine messaging with RabbitMQ transport and durable outbox
        private static WebApplicationBuilder AddWolverineMessaging(this WebApplicationBuilder builder)
        {
            builder.Host.UseWolverine(opts =>
            {
                opts.UseSystemTextJsonForSerialization();
                opts.ApplicationAssembly = typeof(Program).Assembly;
                opts.Discovery.IncludeAssembly(typeof(GamePurchasedRequestHandler).Assembly);

                // -------------------------------
                // Define schema for Wolverine durability and Postgres persistence
                // -------------------------------
                const string wolverineSchema = "wolverine";
                opts.Durability.MessageStorageSchemaName = wolverineSchema;

                // -------------------------------
                // Persist Wolverine messages in Postgres using the same schema
                // -------------------------------
                opts.PersistMessagesWithPostgresql(
                        PostgresHelper.Build(builder.Configuration).ConnectionString,
                        wolverineSchema
                    );

                opts.Policies.OnException<Exception>().RetryTimes(5);

                // -------------------------------
                // Enable durable local queues and auto transaction application
                // -------------------------------
                opts.Policies.UseDurableLocalQueues();
                opts.Policies.AutoApplyTransactions();

                // -------------------------------
                // Load and configure message broker
                // -------------------------------
                var broker = MessageBrokerHelper.Build(builder.Configuration);

                switch (broker.Type)
                {
                    case BrokerType.RabbitMQ when broker.RabbitMqSettings is { } mq:
                        var rabbitOpts = opts.UseRabbitMq(factory =>
                        {
                            factory.Uri = new Uri(mq.ConnectionString);
                            factory.VirtualHost = mq.VirtualHost;

                            //Metadata for monitoring and tracing
                            factory.ClientProperties["application"] = $"TC.CloudGames.Payments.Api";
                            factory.ClientProperties["environment"] = builder.Environment.EnvironmentName;
                        });

                        if (mq.AutoProvision) rabbitOpts.AutoProvision();
                        if (mq.UseQuorumQueues) rabbitOpts.UseQuorumQueues();
                        if (mq.AutoPurgeOnStartup) rabbitOpts.AutoPurgeOnStartup();

                        // Durable outbox
                        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
                        opts.Policies.UseDurableInboxOnAllListeners();

                        var exchangeName = $"{mq.Exchange}-exchange";
                        // Register messages
                        opts.PublishMessage<EventContext<GamePaymentStatusUpdateIntegrationEvent>>()
                            .ToRabbitExchange(exchangeName)
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        // Declara fila para eventos de Games
                        opts.ListenToRabbitQueue($"payments.{mq.ListenGameExchange}-queue", configure =>
                        {
                            configure.IsDurable = mq.Durable;
                            configure.BindExchange(exchangeName: $"{mq.ListenGameExchange}-exchange");
                        })
                        .UseDurableInbox();

                        break;

                    case BrokerType.AzureServiceBus when broker.ServiceBusSettings is { } sb:
                        var azureOpts = opts.UseAzureServiceBus(sb.ConnectionString);

                        if (sb.AutoProvision) azureOpts.AutoProvision();
                        if (sb.AutoPurgeOnStartup) azureOpts.AutoPurgeOnStartup();
                        if (sb.UseControlQueues) azureOpts.EnableWolverineControlQueues();

                        // Durable outbox for all sending endpoints
                        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
                        opts.Policies.UseDurableInboxOnAllListeners();

                        opts.RegisterPaymentEvents();

                        // GAMES API EVENTS -------------------------------
                        opts.RegisterGameEvents();

                        var topicName = $"{sb.TopicName}-topic";
                        opts.PublishMessage<EventContext<GamePaymentStatusUpdateIntegrationEvent>>()
                            .ToAzureServiceBusTopic(topicName)
                            .CustomizeOutgoing(e =>
                            {
                                e.Headers["DomainAggregate"] = "PaymentAggregate";
                            })
                            .BufferedInMemory()
                            .UseDurableOutbox()
                            .CircuitBreaking(configure =>
                            {
                                configure.FailuresBeforeCircuitBreaks = 5;
                                configure.MaximumEnvelopeRetryStorage = 10;
                            });

                        // Declare subscription for GAMES events
                        opts.ListenToAzureServiceBusSubscription(
                            subscriptionName: $"payments.{sb.GamesTopicName}-subscription",
                            configureSubscriptions: configure =>
                            {
                                configure.TopicName = $"{sb.GamesTopicName}-topic";
                                configure.MaxDeliveryCount = sb.MaxDeliveryCount;
                                configure.DeadLetteringOnMessageExpiration = sb.EnableDeadLettering;
                            },
                            configureSubscriptionRule: configure =>
                            {
                                configure.Name = "GamesDomainAggregateFilter";
                                configure.Filter = new SqlRuleFilter("DomainAggregate = 'GameAggregate'");
                            })
                        .FromTopic($"{sb.GamesTopicName}-topic")
                        .UseDurableInbox();

                        break;
                }
            })
            .ConfigureLogging(configureLogging: config =>
            {
                config.AddDebug().AddConsole().SetMinimumLevel(LogLevel.Debug);
            });

            // -------------------------------
            // Ensure all messaging resources and schema are created at startup
            // -------------------------------
            builder.Services.AddResourceSetupOnStartup();

            return builder;
        }

        // 1) Configure Marten with event sourcing, projections, and Wolverine integration
        private static IServiceCollection AddMartenEventSourcing(this IServiceCollection services)
        {
            services.AddMarten(serviceProvider =>
            {
                var connProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();

                var options = new StoreOptions();
                options.Connection(connProvider.ConnectionString);
                options.Logger(new ConsoleMartenLogger());
                options.OpenTelemetry.TrackConnections = Marten.Services.TrackLevel.Normal;
                options.OpenTelemetry.TrackEventCounters();

                options.Events.DatabaseSchemaName = "events";
                options.DatabaseSchemaName = "documents";

                options.CreateDatabasesForTenants(c =>
                {
                    c.MaintenanceDatabase(connProvider.MaintenanceConnectionString);
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });

                // Snapshot automático do aggregate (para acelerar LoadAsync)
                options.Projections.Snapshot<PaymentAggregate>(SnapshotLifecycle.Inline);

                return options;
            })
            .UseLightweightSessions()
            .IntegrateWithWolverine(cfg =>
            {
                cfg.UseWolverineManagedEventSubscriptionDistribution = true;
            })
            .ApplyAllDatabaseChangesOnStartup();

            return services;
        }

        public static IServiceCollection ConfigureAppSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
            services.Configure<AzureServiceBusOptions>(configuration.GetSection("AzureServiceBus"));
            services.Configure<PostgresOptions>(configuration.GetSection("Database"));

            return services;
        }
    }

    /// <summary>
    /// Processor to ignore noisy or unsafe activities.
    /// </summary>
    public class FilteringActivityProcessor : BaseProcessor<Activity>
    {
        private readonly Func<Activity, bool> _filter;

        public FilteringActivityProcessor(Func<Activity, bool> filter)
        {
            _filter = filter;
        }

        public override void OnEnd(Activity data)
        {
            if (_filter(data))
            {
                base.OnEnd(data);
            }
        }
    }
}
