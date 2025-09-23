namespace TC.CloudGames.Payments.Application.Abstractions.Ports
{
    public interface IPaymentRepository
    {
        Task SaveAsync(PaymentAggregate aggregate, CancellationToken cancellationToken = default);
        Task<PaymentAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
