using OrderProcessing.Models.Results;

namespace OrderProcessing.Services;

public interface IPaymentGateway
{
  Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken cancellationToken);
  Task RefundAsync(string transactionId, CancellationToken cancellationToken);
}
