using OrderProcessing.Models.Results;

namespace OrderProcessing.SyncServices;

public interface ISyncPaymentGateway
{
  PaymentResult ProcessPayment(int orderId, decimal amount);
  void Refund(string transactionId);
}
