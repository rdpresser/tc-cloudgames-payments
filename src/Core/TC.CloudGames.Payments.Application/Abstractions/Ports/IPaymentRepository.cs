using TC.CloudGames.SharedKernel.Application.Ports;

namespace TC.CloudGames.Payments.Application.Abstractions.Ports
{
    public interface IPaymentRepository : IBaseRepository<PaymentAggregate>
    {
        Task<PaymentAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
