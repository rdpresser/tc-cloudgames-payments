global using FluentValidation;
global using Microsoft.Extensions.DependencyInjection;
global using System.Diagnostics.CodeAnalysis;
global using TC.CloudGames.Contracts.Events.Games;
global using TC.CloudGames.Contracts.Events.Payments;
global using TC.CloudGames.Payments.Application.Abstractions.Ports;
global using TC.CloudGames.Payments.Domain.Aggregates;
global using TC.CloudGames.SharedKernel.Infrastructure.Messaging;
global using Wolverine;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("TC.CloudGames.Payments.Unit.Tests")]