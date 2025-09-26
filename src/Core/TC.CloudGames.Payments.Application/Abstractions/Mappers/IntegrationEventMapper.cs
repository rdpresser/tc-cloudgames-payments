using TC.CloudGames.Contracts.Events;
using TC.CloudGames.SharedKernel.Domain.Aggregate;
using TC.CloudGames.SharedKernel.Domain.Events;

namespace TC.CloudGames.Payments.Application.Abstractions.Mappers
{
    public static class IntegrationEventMapper
    {
        /// <summary>
        /// Maps multiple domain events to their respective integration events and wraps them in EventContext
        /// Preserves the concrete type of integration events to include all properties in serialization.
        /// </summary>
        /// <typeparam name="TAggregate">Aggregate type</typeparam>
        /// <param name="domainEvents">All domain events to map</param>
        /// <param name="aggregate">The aggregate root</param>
        /// <param name="userContext">Current user context for headers</param>
        /// <param name="handlerName">Optional handler name for automatic source generation</param>
        /// <param name="mappings">
        /// Dictionary mapping from domain event Type → function to convert it to concrete integration event
        /// </param>
        /// <returns>IEnumerable of EventContext of the integration events ready to publish</returns>
        public static IEnumerable<EventContext<TIntegrationEvent>> MapToIntegrationEvents<TAggregate, TIntegrationEvent>(
            this IEnumerable<BaseDomainEvent> domainEvents,
            TAggregate aggregate,
            (string Id, bool IsAuthenticated, string? CorrelationId) userContext,
            string? handlerName = null,
            IDictionary<Type, Func<BaseDomainEvent, TIntegrationEvent>>? mappings = null
        )
            where TAggregate : BaseAggregateRoot
            where TIntegrationEvent : BaseIntegrationEvent
        {
            if (mappings == null) yield break;

            foreach (var domainEvent in domainEvents)
            {
                var type = domainEvent.GetType();
                if (!mappings.TryGetValue(type, out var mapFunc)) continue;

                var integrationEvent = mapFunc(domainEvent);

                yield return EventContext<TIntegrationEvent>.Create<PaymentAggregate>(
                    data: integrationEvent,
                    aggregateId: aggregate.Id,
                    userId: userContext.Id,
                    isAuthenticated: userContext.IsAuthenticated,
                    correlationId: userContext.CorrelationId,
                    source: $"Payment.API.{handlerName ?? "UnknownHandler"}.{integrationEvent.GetType().Name}"
                );
            }
        }
    }
}