using Shouldly;
using TC.CloudGames.Payments.Domain.Aggregates;
using TC.CloudGames.SharedKernel.Domain.Events;
using static TC.CloudGames.Payments.Domain.Aggregates.PaymentAggregate;
namespace TC.CloudGames.Payments.Unit.Tests.Domain.Aggregates.Payment;

public class PaymentAggregateBehaviorTests
{
    [Fact]
    public void PaymentAggregate_AfterCreation_ShouldBeActive()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void PaymentAggregate_AfterCreation_ShouldHavePurchaseDate()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.PurchaseDate.ShouldNotBe(default);
        result.Value.PurchaseDate.ShouldBeGreaterThanOrEqualTo(beforeTime);
        result.Value.PurchaseDate.ShouldBeLessThanOrEqualTo(afterTime);
    }

    [Fact]
    public void PaymentAggregate_AfterCreation_ShouldHaveCreatedAt()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CreatedAt.ShouldNotBe(default);
        result.Value.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeTime);
        result.Value.CreatedAt.ShouldBeLessThanOrEqualTo(afterTime);
    }

    [Fact]
    public void PaymentAggregate_AfterCreation_ShouldHaveUncommittedEvents()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.UncommittedEvents.ShouldNotBeEmpty();
        result.Value.UncommittedEvents.Count.ShouldBe(1);
        result.Value.UncommittedEvents[0].ShouldBeOfType<PaymentStatusUpdateDomainEvent>();
    }

    [Fact]
    public void PaymentAggregate_UncommittedEvents_ShouldBeReadOnly()
    {
        // Arrange
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        // Act & Assert
        result.IsSuccess.ShouldBeTrue();
        (result.Value.UncommittedEvents is List<object>).ShouldBeFalse();
        result.Value.UncommittedEvents.ShouldBeAssignableTo<IReadOnlyList<object>>();
    }

    [Fact]
    public void MarkEventsAsCommitted_ShouldClearUncommittedEvents()
    {
        // Arrange
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);
        
        var aggregate = result.Value;
        aggregate.UncommittedEvents.Count.ShouldBe(1);

        // Act
        aggregate.MarkEventsAsCommitted();

        // Assert
        aggregate.UncommittedEvents.ShouldBeEmpty();
    }

    [Fact]
    public void PaymentAggregate_Properties_ShouldBeSettableAfterCreation()
    {
        // Arrange
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        var aggregate = result.Value;
        var originalAmount = aggregate.Amount;
        var originalIsApproved = aggregate.IsApproved;

        // Act - Simulate property changes (if needed for business logic)
        aggregate.Amount = 149.99m;
        aggregate.IsApproved = false;

        // Assert
        aggregate.Amount.ShouldBe(149.99m);
        aggregate.IsApproved.ShouldBeFalse();
        aggregate.Amount.ShouldNotBe(originalAmount);
        aggregate.IsApproved.ShouldNotBe(originalIsApproved);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "Insufficient funds")]
    [InlineData(false, "Card declined")]
    [InlineData(false, "Invalid payment method")]
    public void PaymentAggregate_WithDifferentApprovalStates_ShouldReflectCorrectState(
        bool isApproved, string? errorMessage)
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, isApproved, errorMessage);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsApproved.ShouldBe(isApproved);
        result.Value.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void PaymentAggregate_WithApprovedPayment_ShouldHaveNullErrorMessage()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsApproved.ShouldBeTrue();
        result.Value.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void PaymentAggregate_WithRejectedPayment_ShouldHaveErrorMessage()
    {
        // Arrange
        var errorMessage = "Payment failed due to insufficient funds";

        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, false, errorMessage);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsApproved.ShouldBeFalse();
        result.Value.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void PaymentAggregate_WithZeroAmount_ShouldSucceed()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Free Game", 0m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(0m);
        result.Value.IsApproved.ShouldBeTrue();
    }

    [Fact]
    public void PaymentAggregate_WithEmptyGameName_ShouldSucceed()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "", 99.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.GameName.ShouldBe("");
    }

    [Fact]
    public void PaymentAggregate_WithEmptyGuid_ShouldSucceed()
    {
        // Arrange & Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.Empty, Guid.Empty, Guid.Empty,
            "Test Game", 99.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(Guid.Empty);
        result.Value.UserId.ShouldBe(Guid.Empty);
        result.Value.GameId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void Apply_WithMultipleEvents_ShouldUpdateStateCorrectly()
    {
        // Arrange
        var aggregate = new PaymentAggregate();
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var firstEvent = new PaymentStatusUpdateDomainEvent(
            paymentId, userId, gameId, "Game 1", 50m, false, "First attempt failed");

        var secondEvent = new PaymentStatusUpdateDomainEvent(
            paymentId, userId, gameId, "Game 1", 50m, true, null);

        // Act
        aggregate.Apply(firstEvent);
        aggregate.Apply(secondEvent);

        // Assert
        aggregate.Id.ShouldBe(paymentId);
        aggregate.UserId.ShouldBe(userId);
        aggregate.GameId.ShouldBe(gameId);
        aggregate.GameName.ShouldBe("Game 1");
        aggregate.Amount.ShouldBe(50m);
        aggregate.IsApproved.ShouldBeTrue(); // Last event wins
        aggregate.ErrorMessage.ShouldBeNull(); // Last event wins
    }

    [Fact]
    public void ApplyEvent_WithPaymentStatusUpdateDomainEvent_ShouldCallApply()
    {
        // Arrange
        var aggregate = new PaymentAggregate();
        var @event = new PaymentStatusUpdateDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 99.99m, true, null);

        // Act
        // Use reflection to call the private ApplyEvent method
        var method = typeof(PaymentAggregate).GetMethod("ApplyEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method!.Invoke(aggregate, new object[] { @event });

        // Assert
        aggregate.UserId.ShouldBe(@event.UserId);
        aggregate.GameId.ShouldBe(@event.GameId);
        aggregate.GameName.ShouldBe(@event.GameName);
        aggregate.Amount.ShouldBe(@event.Amount);
        aggregate.IsApproved.ShouldBe(@event.Success);
        aggregate.ErrorMessage.ShouldBe(@event.ErrorMessage);
    }

    [Fact]
    public void ApplyEvent_WithUnknownEventType_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new PaymentAggregate();
        var unknownEvent = new UnknownDomainEvent(Guid.NewGuid());

        // Act & Assert
        Should.NotThrow(() =>
        {
            var method = typeof(PaymentAggregate).GetMethod("ApplyEvent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(aggregate, new object[] { unknownEvent });
        });
    }

    // Helper class for testing unknown event type
    private record UnknownDomainEvent(Guid Id) : BaseDomainEvent(Id, DateTimeOffset.UtcNow);
}
