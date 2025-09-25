namespace TC.CloudGames.Payments.Application.MessageBrokerHandlers
{
    public class ChargePaymentRequestHandler
    {
        private readonly IPaymentRepository _paymentRepository;

        public ChargePaymentRequestHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        }

        public async Task<ChargePaymentResponse> Handle(ChargePaymentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var payment = new PaymentAggregate
                {
                    Id = Guid.NewGuid(),
                    GameId = request.GameId,
                    UserId = request.UserId,
                    Amount = request.Amount,
                    GameName = string.Empty,
                    PurchaseDate = DateTimeOffset.UtcNow
                };

                await _paymentRepository.SaveAsync(payment, cancellationToken);

                return new ChargePaymentResponse(
                    success: true,
                    paymentId: payment.Id,
                    errorMessage: null
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


        ////    //Wolverine RPC convention: método deve retornar a response esperada
        ////    public async ValueTask<ChargePaymentResponse> Handle(
        ////       ChargePaymentRequest request,
        ////       IMessageContext context,
        ////       CancellationToken cancellationToken = default)
        ////    {
        ////       try
        ////       {
        ////           var payment = new PaymentAggregate
        ////           {
        ////               Id = Guid.NewGuid(),
        ////               GameId = request.GameId,
        ////               UserId = request.UserId,
        ////               Amount = request.Amount,
        ////               GameName = string.Empty, //adicionar gamename no RPC
        ////               PurchaseDate = DateTimeOffset.UtcNow
        ////           };

        ////           await _paymentRepository.SaveAsync(payment, cancellationToken);

        ////           var response = new ChargePaymentResponse(
        ////               success: true,
        ////               paymentId: payment.Id,
        ////               errorMessage: null
        ////           );
        ////           await context.RespondToSenderAsync(response);
        ////           return response;
        ////       }
        ////       catch (Exception ex)
        ////       {
        ////           var response = new ChargePaymentResponse(
        ////               success: false,
        ////               paymentId: null,
        ////               errorMessage: ex.Message
        ////           );
        ////           await context.RespondToSenderAsync(response);
        ////           return response;
        ////       }
        ////    }
    }
}
