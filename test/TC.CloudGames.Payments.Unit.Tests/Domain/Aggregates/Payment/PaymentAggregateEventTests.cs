using Shouldly;
using TC.CloudGames.Payments.Domain.Aggregates;
using static TC.CloudGames.Payments.Domain.Aggregates.PaymentAggregate;

namespace TC.CloudGames.Payments.Unit.Tests.Domain.Aggregates.Payment;

public class PaymentAggregateEventTests
{
    [Fact]
    public void Create_ShouldRaisePaymentStatusUpdateDomainEvent()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var aggregate = result.Value;
        aggregate.UncommittedEvents.ShouldNotBeEmpty();
        aggregate.UncommittedEvents.Count.ShouldBe(1);
        aggregate.UncommittedEvents[0].ShouldBeOfType<PaymentStatusUpdateDomainEvent>();
    }

    [Fact]
    public void Apply_ShouldPopulateAllPropertiesFromEvent()
    {
        // Arrange
        var aggregate = new PaymentAggregate();
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var gameName = "Test Game";
        var amount = 79.99m;
        var isApproved = true;
        string? errorMessage = null;

        var @event = new PaymentStatusUpdateDomainEvent(
            paymentId, userId, gameId, gameName, amount, isApproved, errorMessage);

        // Act
        aggregate.Apply(@event);

        // Assert
        aggregate.Id.ShouldBe(paymentId);
        aggregate.UserId.ShouldBe(userId);
        aggregate.GameId.ShouldBe(gameId);
        aggregate.GameName.ShouldBe(gameName);
        aggregate.Amount.ShouldBe(amount);
        aggregate.IsApproved.ShouldBe(isApproved);
        aggregate.ErrorMessage.ShouldBe(errorMessage);
        aggregate.PurchaseDate.ShouldNotBe(default);
        aggregate.CreatedAt.ShouldNotBe(default);
        aggregate.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void PaymentStatusUpdateDomainEvent_ShouldAutoPopulateTimestamp()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var @event = new PaymentStatusUpdateDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test", 50m, true, null);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        @event.OccurredOn.ShouldBeGreaterThanOrEqualTo(beforeTime);
        @event.OccurredOn.ShouldBeLessThanOrEqualTo(afterTime);
    }
}
