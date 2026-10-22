using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Models.Results;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.SyncServices;

public class SyncPaymentGateway : ISyncPaymentGateway
{
  private static readonly Lock _paymentLock = new();
  private readonly HashSet<int> _timeoutOrderIds = [];
  private readonly HashSet<int> _declineOrderIds = [];
  private readonly JsonFileStore _jsonFileStore;

  public SyncPaymentGateway() : this(null, null) { }

  public SyncPaymentGateway(
    IEnumerable<int>? timeoutOrderIds = null,
    IEnumerable<int>? declineOrderIds = null)
  {
    _jsonFileStore = new JsonFileStore();

    if (timeoutOrderIds != null)
    {
      foreach (var id in timeoutOrderIds)
      {
        _timeoutOrderIds.Add(id);
      }
    }

    if (declineOrderIds != null)
    {
      foreach (var id in declineOrderIds)
      {
        _declineOrderIds.Add(id);
      }
    }
  }

  public PaymentResult ProcessPayment(int orderId, decimal amount)
  {
    try
    {
      if (_timeoutOrderIds.Contains(orderId))
      {
        Thread.Sleep(5000);
      }
      else
      {
        Thread.Sleep(700);
      }
    }
    catch (ThreadInterruptedException)
    {
      Logger.Error<SyncPaymentGateway>($"Payment processing for Order #{orderId} was cancelled or timed out.");
      throw new TimeoutException("Payment gateway timeout (3s)");
    }

    if (_declineOrderIds.Contains(orderId))
    {
      var declinedResult = new PaymentResult
      {
        Success = false,
        Status = PaymentStatus.Declined,
        Message = "Card declined by issuer",
        TransactionId = null
      };

      var declinedTransaction = new PaymentTransaction
      {
        OrderId = orderId,
        Amount = amount,
        Status = PaymentStatus.Declined,
        TransactionId = null,
        CreatedAt = DateTime.Now
      };

      lock (_paymentLock)
      {
        var payments = _jsonFileStore.Read<List<PaymentTransaction>>(DataFiles.Payments);
        payments ??= [];
        payments.Add(declinedTransaction);
        _jsonFileStore.Write(DataFiles.Payments, payments);
      }

      return declinedResult;
    }

    bool isCaptured = amount > 0 && orderId > 0;
    string? transactionId = isCaptured ? ("TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper()) : null;
    PaymentStatus status = isCaptured ? PaymentStatus.Captured : PaymentStatus.Declined;
    string message = isCaptured ? "Payment captured successfully." : "Payment declined.";

    var transaction = new PaymentTransaction
    {
      OrderId = orderId,
      Amount = amount,
      Status = status,
      TransactionId = transactionId,
      CreatedAt = DateTime.Now
    };

    lock (_paymentLock)
    {
      var payments = _jsonFileStore.Read<List<PaymentTransaction>>(DataFiles.Payments);
      payments ??= [];
      payments.Add(transaction);
      _jsonFileStore.Write(DataFiles.Payments, payments);
    }

    return new PaymentResult
    {
      Success = isCaptured,
      TransactionId = transactionId,
      Status = status,
      Message = message
    };
  }

  public void Refund(string transactionId)
  {
    if (string.IsNullOrWhiteSpace(transactionId))
    {
      Logger.Error<SyncPaymentGateway>("Invalid transaction ID for refund.");
      return;
    }

    lock (_paymentLock)
    {
      Thread.Sleep(200);

      var payments = _jsonFileStore.Read<List<PaymentTransaction>>(DataFiles.Payments);
      payments ??= [];

      Thread.Sleep(300);

      bool found = false;
      foreach (var payment in payments)
      {
        if (string.Equals(payment.TransactionId, transactionId, StringComparison.OrdinalIgnoreCase))
        {
          payment.Status = PaymentStatus.Refunded;
          found = true;
          break;
        }
      }

      if (found)
      {
        _jsonFileStore.Write(DataFiles.Payments, payments);
        Logger.Success<SyncPaymentGateway>($"Transaction '{transactionId}' refunded successfully.");
      }
      else
      {
        Logger.Error<SyncPaymentGateway>($"Transaction '{transactionId}' not found for refund.");
      }
    }
  }
}
