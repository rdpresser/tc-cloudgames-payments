using Ardalis.Result;
using TC.CloudGames.SharedKernel.Domain.Aggregate;
using TC.CloudGames.SharedKernel.Domain.Events;

namespace TC.CloudGames.Payments.Domain.Aggregates
{
    public class PaymentAggregate : BaseAggregateRoot
    {
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsApproved { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset PurchaseDate { get; set; }

        public PaymentAggregate() : base() { }

        // Construtor privado para factories
        private PaymentAggregate(Guid id) : base(id) { }

        public static Result<PaymentAggregate> CreateFromPrimitives(Guid id, Guid userId, Guid gameId, string gameName, decimal amount, bool isApproved, string? errorMessage)
        {
            var aggregate = new PaymentAggregate(id);
            var @event = new PaymentStatusUpdateDomainEvent(aggregate.Id, userId, gameId, gameName, amount, isApproved, errorMessage, DateTimeOffset.UtcNow);
            aggregate.ApplyEvent(@event);
            return Result<PaymentAggregate>.Success(aggregate);
        }

        private void ApplyEvent(BaseDomainEvent @event)
        {
            AddNewEvent(@event);
            switch (@event)
            {
                case PaymentStatusUpdateDomainEvent createdEvent: Apply(createdEvent); break;
            }
        }

        public void Apply(PaymentStatusUpdateDomainEvent @event)
        {
            SetId(@event.AggregateId);
            UserId = @event.UserId;
            GameId = @event.GameId;
            GameName = @event.GameName;
            Amount = @event.Amount;
            IsApproved = @event.Success;
            ErrorMessage = @event.ErrorMessage;
            PurchaseDate = @event.OccurredOn;
            SetCreatedAt(@event.OccurredOn);
            SetActivate();
        }

        public record PaymentStatusUpdateDomainEvent(
            Guid AggregateId,
            Guid UserId,
            Guid GameId,
            string GameName,
            decimal Amount,
            bool Success,
            string? ErrorMessage,
            DateTimeOffset OccurredOn) : BaseDomainEvent(AggregateId, OccurredOn);
    }
}
