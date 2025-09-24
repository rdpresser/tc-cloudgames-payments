namespace TC.CloudGames.Payments.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IDocumentSession _session;

        public PaymentRepository(IDocumentSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task<PaymentAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _session.LoadAsync<PaymentAggregate>(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task SaveAsync(PaymentAggregate aggregate, CancellationToken cancellationToken = default)
        {
            _session.Store(aggregate);
            await _session.SaveChangesAsync(cancellationToken);
        }
    }
}
