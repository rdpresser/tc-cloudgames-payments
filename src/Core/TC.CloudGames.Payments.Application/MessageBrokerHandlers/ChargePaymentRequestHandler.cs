namespace TC.CloudGames.Payments.Application.MessageBrokerHandlers
{
    public class ChargePaymentRequestHandler
    {
        private readonly IPaymentRepository _paymentRepository;

        public ChargePaymentRequestHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<ChargePaymentResponse> Handle(ChargePaymentRequest @event)
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

                await _paymentRepository.SaveAsync(payment);

                return new ChargePaymentResponse(
                    success: true,
                    paymentId: payment.Id,
                    errorMessage: string.Empty
                );
            }
            catch (Exception ex)
            {
                return new ChargePaymentResponse(
                    success: false,
                    paymentId: null,
                    errorMessage: ex.Message
                );
            }
        }
    }
}
