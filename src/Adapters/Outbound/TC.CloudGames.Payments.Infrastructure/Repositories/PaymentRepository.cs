using TC.CloudGames.SharedKernel.Infrastructure.Repositories;

namespace TC.CloudGames.Payments.Infrastructure.Repositories
{
    public class PaymentRepository : BaseRepository<PaymentAggregate>, IPaymentRepository
    {
        private readonly IDocumentSession _session;

        public PaymentRepository(IDocumentSession session)
            : base(session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public override Task<IEnumerable<PaymentAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _session.LoadAsync<PaymentAggregate>(id, cancellationToken).ConfigureAwait(false);
        }
    }
}
