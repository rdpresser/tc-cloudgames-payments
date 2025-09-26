using Microsoft.Extensions.Logging;
using TC.CloudGames.Payments.Application.Abstractions.Mappers;
using TC.CloudGames.SharedKernel.Domain.Events;
using Wolverine.Marten;

namespace TC.CloudGames.Payments.Application.MessageBrokerHandlers
{
    public class GamePurchasedRequestHandler
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMartenOutbox _outbox;
        private readonly ILogger<GamePurchasedRequestHandler> _logger;

        public GamePurchasedRequestHandler(IPaymentRepository paymentRepository, IMartenOutbox outbox, ILogger<GamePurchasedRequestHandler> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(EventContext<GamePurchasedIntegrationEvent> @event, CancellationToken cancellationToken = default)
        {
            try
            {
                //1. Map event to domain aggregate
                var aggregate = MapEventToAggregate(@event, isApproved: true, errorMessage: null);

                //2. Persist domain aggregate 
                await _paymentRepository.SaveAsync(aggregate, cancellationToken).ConfigureAwait(false);

                //3. Publish payment integration event to game api
                await PublishIntegrationEventsAsync(aggregate, @event).ConfigureAwait(false);

                //4. Commit transaction with outbox pattern
                await _paymentRepository.CommitAsync(aggregate, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Purchase charged completed successfully for User {UserId}, Game {GameId}", @event.UserId, aggregate.GameId);
            }
            catch (Exception ex)
            {
                var aggregate = MapEventToAggregate(@event, isApproved: false, ex.Message);

                await PublishIntegrationEventsAsync(aggregate, @event).ConfigureAwait(false);
            }
        }

        // Rename the static method to avoid name collision with the class
        private static PaymentAggregate MapEventToAggregate(EventContext<GamePurchasedIntegrationEvent> @event, bool isApproved, string? errorMessage)
        {
            return PaymentAggregate.CreateFromPrimitives(
                id: @event.EventData.PaymentId,
                userId: @event.EventData.UserId,
                gameId: @event.EventData.GameId,
                gameName: @event.EventData.GameName,
                amount: @event.EventData.Amount,
                isApproved: isApproved,
                errorMessage: errorMessage
            );
        }

        private static GamePaymentStatusUpdateIntegrationEvent ToIntegrationEvent(PaymentAggregate.PaymentStatusUpdateDomainEvent domainEvent, Guid aggregateId)
        => new(
                AggregateId: aggregateId, //UserGameLibraryAggregate Id
                UserId: domainEvent.UserId,
                GameId: domainEvent.GameId,
                PaymentId: domainEvent.AggregateId,
                Status: domainEvent.Success ? "Charged" : "Not Charged",
                Success: domainEvent.Success,
                ErrorMessage: domainEvent.ErrorMessage,
                OccurredOn: DateTimeOffset.UtcNow
            );

        protected async Task PublishIntegrationEventsAsync(PaymentAggregate aggregate, EventContext<GamePurchasedIntegrationEvent> @event)
        {
            var mappings = new Dictionary<Type, Func<BaseDomainEvent, GamePaymentStatusUpdateIntegrationEvent>>
            {
                { typeof(PaymentAggregate.PaymentStatusUpdateDomainEvent), e => ToIntegrationEvent((PaymentAggregate.PaymentStatusUpdateDomainEvent)e, @event.EventData.AggregateId) }
            };

            var integrationEvents = aggregate.UncommittedEvents
                .MapToIntegrationEvents(
                    aggregate: aggregate,
                    userContext: (@event.UserId!, @event.IsAuthenticated, @event.CorrelationId),
                    handlerName: nameof(GamePurchasedRequestHandler),
                    mappings: mappings
                );

            foreach (var evt in integrationEvents)
            {
                _logger.LogDebug(
                    "Queueing integration event {EventType} for user {UserId}, game {GameId} in Marten outbox",
                    evt.EventData.GetType().Name,
                    evt.AggregateId,
                    evt.EventData.RelatedIds!["GameId"]);

                await _outbox.PublishAsync(evt).ConfigureAwait(false);
            }
        }
    }
}
