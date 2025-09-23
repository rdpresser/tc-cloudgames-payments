global using Marten;
global using Microsoft.Extensions.DependencyInjection;
global using System.Diagnostics.CodeAnalysis;
global using TC.CloudGames.Payments.Application.Abstractions.Ports;
global using TC.CloudGames.Payments.Domain.Aggregates;
global using TC.CloudGames.Payments.Infrastructure.Repositories;
global using TC.CloudGames.SharedKernel.Infrastructure.Authentication;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.Provider;
global using TC.CloudGames.SharedKernel.Infrastructure.Caching.Service;
global using TC.CloudGames.SharedKernel.Infrastructure.Clock;
global using TC.CloudGames.SharedKernel.Infrastructure.Database;
global using TC.CloudGames.SharedKernel.Infrastructure.UserClaims;
//**//
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("TC.CloudGames.Payments.Unit.Tests")]