using Azure.Messaging.ServiceBus.Administration;
using Marten.Events.Projections;
using TC.CloudGames.Contracts.Events.Payments;
using TC.CloudGames.SharedKernel.Infrastructure.Messaging;

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
                .AddCustomHealthCheck();

            //services// Add custom telemetry services
            //    .AddSingleton<UserMetrics>()
            //services.AddCustomOpenTelemetry()

            return services;
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

        // 2) Configure Wolverine messaging with RabbitMQ transport and durable outbox
        private static WebApplicationBuilder AddWolverineMessaging(this WebApplicationBuilder builder)
        {
            builder.Host.UseWolverine(opts =>
            {
                opts.UseSystemTextJsonForSerialization();
                opts.ApplicationAssembly = typeof(Program).Assembly;
                opts.Discovery.IncludeAssembly(typeof(GamePurchasedRequestHandler).Assembly);
                Console.WriteLine($"Handler discovery: {opts.DescribeHandlerMatch(typeof(GamePurchasedRequestHandler))}");

                // -------------------------------
                // Define schema for Wolverine durability and Postgres persistence
                // -------------------------------
                const string wolverineSchema = "wolverine";
                opts.Durability.MessageStorageSchemaName = wolverineSchema;

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
                            .CustomizeOutgoing(e => e.Headers["DomainAggregate"] = "PaymentAggregate")
                            .BufferedInMemory()
                            .UseDurableOutbox();

                        // Declare subscription for PAYMENT events
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
                                configure.Name = "PaymentsDomainAggregateFilter";
                                configure.Filter = new SqlRuleFilter("DomainAggregate = 'GameAggregate'");
                            })
                        .FromTopic($"{sb.GamesTopicName}-topic")
                        .UseDurableInbox();

                        break;
                }

                // -------------------------------
                // Persist Wolverine messages in Postgres using the same schema
                // -------------------------------
                opts.PersistMessagesWithPostgresql(
                        PostgresHelper.Build(builder.Configuration).ConnectionString,
                        wolverineSchema
                    );
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
}
