global using Ardalis.Result;
global using Azure.Messaging.ServiceBus.Administration;
global using HealthChecks.UI.Client;
global using JasperFx.Resources;
global using Marten;
global using Marten.Events.Projections;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Npgsql;
global using OpenTelemetry;
global using OpenTelemetry.Logs;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Resources;
global using OpenTelemetry.Trace;
global using Serilog;
global using Serilog.Core;
global using Serilog.Enrichers.Span;
global using Serilog.Events;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using TC.CloudGames.Contracts.Events.Payments;
global using TC.CloudGames.Messaging.Extensions;
global using TC.CloudGames.Payments.Api.Extensions;
global using TC.CloudGames.Payments.Api.Telemetry;
global using TC.CloudGames.Payments.Application.MessageBrokerHandlers;
global using TC.CloudGames.Payments.Domain.Aggregates;
global using TC.CloudGames.Payments.Infrastructure;
global using TC.CloudGames.SharedKernel.Extensions;
global using TC.CloudGames.SharedKernel.Infrastructure.Database;
global using TC.CloudGames.SharedKernel.Infrastructure.Database.Initializer;
global using TC.CloudGames.SharedKernel.Infrastructure.MessageBroker;
global using TC.CloudGames.SharedKernel.Infrastructure.Messaging;
global using TC.CloudGames.SharedKernel.Infrastructure.Middleware;
global using Wolverine;
global using Wolverine.AzureServiceBus;
global using Wolverine.ErrorHandling;
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