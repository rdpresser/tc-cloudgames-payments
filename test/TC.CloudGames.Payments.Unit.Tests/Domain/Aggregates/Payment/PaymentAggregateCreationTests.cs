using Shouldly;
using TC.CloudGames.Payments.Domain.Aggregates;

namespace TC.CloudGames.Payments.Unit.Tests.Domain.Aggregates.Payment;

public class PaymentAggregateCreationTests
{
    [Fact]
    public void CreateFromPrimitives_WithValidData_ShouldSucceed()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var gameName = "Elden Ring";
        var amount = 299.99m;
        var isApproved = true;
        string? errorMessage = null;

        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            paymentId, userId, gameId, gameName, amount, isApproved, errorMessage);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var aggregate = result.Value;

        aggregate.Id.ShouldBe(paymentId);
        aggregate.UserId.ShouldBe(userId);
        aggregate.GameId.ShouldBe(gameId);
        aggregate.GameName.ShouldBe(gameName);
        aggregate.Amount.ShouldBe(amount);
        aggregate.IsApproved.ShouldBeTrue();
        aggregate.ErrorMessage.ShouldBeNull();
        aggregate.PurchaseDate.ShouldNotBe(default);
        aggregate.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9.99)]
    [InlineData(59.99)]
    [InlineData(999.99)]
    public void CreateFromPrimitives_WithVariousValidAmounts_ShouldSucceed(decimal amount)
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            paymentId, userId, gameId, "Test Game", amount, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(amount);
    }

    [Fact]
    public void CreateFromPrimitives_WithRejectedPayment_ShouldContainErrorMessage()
    {
        // Arrange
        var errorMessage = "Insufficient funds";

        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 49.99m, false, errorMessage);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsApproved.ShouldBeFalse();
        result.Value.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void CreateFromPrimitives_WithApprovedPayment_ShouldHaveNullErrorMessage()
    {
        // Act
        var result = PaymentAggregate.CreateFromPrimitives(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test Game", 49.99m, true, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsApproved.ShouldBeTrue();
        result.Value.ErrorMessage.ShouldBeNull();
    }
}
