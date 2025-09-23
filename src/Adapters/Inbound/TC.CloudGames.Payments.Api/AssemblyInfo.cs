global using Ardalis.Result;
global using HealthChecks.UI.Client;
global using JasperFx.Resources;
global using Marten;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Serilog;
global using Serilog.Core;
global using Serilog.Enrichers.Span;
global using Serilog.Events;
global using Serilog.Sinks.Grafana.Loki;
global using System.Diagnostics.CodeAnalysis;
global using TC.CloudGames.Messaging.Extensions;
global using TC.CloudGames.Payments.Api.Extensions;
global using TC.CloudGames.Payments.Application.MessageBrokerHandlers;
global using TC.CloudGames.Payments.Domain.Aggregates;
global using TC.CloudGames.Payments.Infrastructure;
global using TC.CloudGames.SharedKernel.Extensions;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.HealthCheck;
global using TC.CloudGames.SharedKernel.Infrastructure.Database;
global using TC.CloudGames.SharedKernel.Infrastructure.Database.Initializer;
global using TC.CloudGames.SharedKernel.Infrastructure.MessageBroker;
global using TC.CloudGames.SharedKernel.Infrastructure.Middleware;
global using Wolverine;
global using Wolverine.AzureServiceBus;
global using Wolverine.Marten;
global using Wolverine.Postgresql;
global using Wolverine.RabbitMQ;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.CloudGames.Payments.Unit.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
//**// REMARK: Required for functional and integration tests to work.
namespace TC.CloudGames.Payments.Api
{
    public partial class Program;
}