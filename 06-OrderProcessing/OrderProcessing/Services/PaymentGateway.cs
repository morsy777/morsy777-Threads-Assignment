using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Models.Results;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Services;

public class PaymentGateway : IPaymentGateway
{
  private static readonly SemaphoreSlim _paymentLock = new(1, 1);
  private readonly JsonFileStore _jsonFileStore;

  public PaymentGateway()
  {
    _jsonFileStore = new JsonFileStore();
  }

  public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);

    bool isCaptured = amount > 0 && orderId > 0;
    string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();
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

    try
    {
      await _paymentLock.WaitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<PaymentGateway>($"Payment processing for Order #{orderId} was cancelled.");
      throw;
    }

    try
    {
      await Task.Delay(300, cancellationToken);

      var payments = await _jsonFileStore.ReadAsync<List<PaymentTransaction>>(DataFiles.Payments, cancellationToken);
      payments ??= [];

      payments.Add(transaction);

      await _jsonFileStore.WriteAsync(DataFiles.Payments, payments, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<PaymentGateway>($"Payment processing for Order #{orderId} was cancelled.");
      throw;
    }
    finally
    {
      _paymentLock.Release();
    }

    return new PaymentResult
    {
      Success = isCaptured,
      TransactionId = transactionId,
      Status = status,
      Message = message
    };
  }

  public async Task RefundAsync(string transactionId, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(transactionId))
    {
      Logger.Error<PaymentGateway>("Invalid transaction ID for refund.");
      return;
    }

    try
    {
      await _paymentLock.WaitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<PaymentGateway>($"Refund for transaction '{transactionId}' was cancelled.");
      return;
    }

    try
    {
      await Task.Delay(200, cancellationToken);

      var payments = await _jsonFileStore.ReadAsync<List<PaymentTransaction>>(DataFiles.Payments, cancellationToken);
      payments ??= [];

      await Task.Delay(300, cancellationToken);

      bool found = false;
      foreach (var payment in payments)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.Equals(payment.TransactionId, transactionId, StringComparison.OrdinalIgnoreCase))
        {
          payment.Status = PaymentStatus.Refunded;
          found = true;
          break;
        }
      }

      if (found)
      {
        await _jsonFileStore.WriteAsync(DataFiles.Payments, payments, cancellationToken);
        Logger.Success<PaymentGateway>($"Transaction '{transactionId}' refunded successfully.");
      }
      else
      {
        Logger.Error<PaymentGateway>($"Transaction '{transactionId}' not found for refund.");
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<PaymentGateway>($"Refund for transaction '{transactionId}' was cancelled.");
      throw;
    }
    finally
    {
      _paymentLock.Release();
    }
  }
}
