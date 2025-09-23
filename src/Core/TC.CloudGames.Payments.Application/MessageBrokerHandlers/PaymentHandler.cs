namespace TC.CloudGames.Payments.Application.MessageBrokerHandlers
{
    public class PaymentHandler : IWolverineHandler
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        ////public async Task<ChargePaymentResponse> HandleAsync(EventContext<ChargePaymentRequest> @event, CancellationToken cancellationToken)
        public async Task<ChargePaymentResponse> HandleAsync(ChargePaymentRequest @event)
        {
            try
            {
                var payment = new PaymentAggregate
                {
                    Id = Guid.NewGuid(),
                    GameId = @event.GameId,
                    UserId = @event.UserId,
                    Amount = @event.Amount,
                    GameName = string.Empty, //adicionar gamename no RPC
                    PurchaseDate = DateTimeOffset.UtcNow
                };

                await _paymentRepository.SaveAsync(payment).ConfigureAwait(false);

                return new ChargePaymentResponse(true, payment.Id, string.Empty);
            }
            catch (Exception ex)
            {
                return new ChargePaymentResponse(false, Guid.Empty, ex.Message);
            }
        }
    }
}
