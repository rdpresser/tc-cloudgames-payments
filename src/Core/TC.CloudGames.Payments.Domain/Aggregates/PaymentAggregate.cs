namespace TC.CloudGames.Payments.Domain.Aggregates
{
    public class PaymentAggregate
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTimeOffset PurchaseDate { get; set; }
    }
}
