using System.Diagnostics;
using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Models.Results;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;
using System.Collections;

namespace OrderProcessing.Services;

public class OrderProcessor
{
  private static readonly Lock _logLock = new();
  private readonly CustomerService _customerService;
  private readonly InventoryService _inventoryService;
  private readonly PricingService _pricingService;
  private readonly IPaymentGateway _paymentGateway;
  private readonly OrderRepository _orderRepository;
  private readonly IEmailService _emailService;
  private readonly JsonFileStore _jsonFileStore;

  public OrderProcessor() : this(null, null, null, null)
  {
  }

  public OrderProcessor(Dictionary<int, int>? emailConfig,
      IEnumerable<int>? timeoutOrderIds,
      IEnumerable<int>? declineOrderIds,
      IEnumerable<int>? failSaveForOrderIds)
  {
      _customerService = new CustomerService();
      _inventoryService = new InventoryService();
      _pricingService = new PricingService(new OrderPricer([]));
      _paymentGateway = new PaymentGateway(timeoutOrderIds ?? [], declineOrderIds ?? []);
      _orderRepository = new OrderRepository(failSaveForOrderIds ?? []);
      _emailService = new EmailService(emailConfig ?? []);
      _jsonFileStore = new JsonFileStore();
  }
  public async Task<bool> ProcessOrderAsync(Order order, CancellationToken cancellationToken = default)
  {
    if (order is null)
      return false;

    bool stockReserved = false;
    bool orderSaved = false;
    var sw = Stopwatch.StartNew();

    try
    {
    // Step1: Customer check
    Customer customer;
    sw.Restart();
    try
    {
      customer = await _customerService.GetCustomerAsync(order.CustomerId, cancellationToken);
    }
    catch (CustomerNotFoundException ex)
    {
      sw.Stop();
      order.Status = OrderStatus.Rejected;
      order.FailureReason = ex.Message;
      LogStep(order.Id, 1, "Customer", $"REJECTED ({ex.Message})", sw.ElapsedMilliseconds, isError: true);
      await _orderRepository.SaveAsync(order, cancellationToken);
      return false;
    }

    if (!customer.IsActive)
    {
      sw.Stop();
      order.Status = OrderStatus.Rejected;
      order.FailureReason = "Customer is inactive";
      LogStep(order.Id, 1, "Customer", "REJECTED (Customer is inactive)", sw.ElapsedMilliseconds, isError: true);
      await _orderRepository.SaveAsync(order, cancellationToken);
      return false;
    }

    sw.Stop();
    LogStep(order.Id, 1, "Customer", $"OK ({customer.Name})", sw.ElapsedMilliseconds);

    // Step2: Stock Reservation
    sw.Restart();
    stockReserved = await _inventoryService.ReserveStockAsync(order, cancellationToken);
    sw.Stop();

    if (!stockReserved)
    {
      if (order.Status != OrderStatus.Rejected)
      {
        order.Status = OrderStatus.Rejected;
        order.FailureReason ??= "Insufficient stock";
        await _orderRepository.SaveAsync(order, cancellationToken);
      }
      LogStep(order.Id, 2, "Stock", $"REJECTED ({order.FailureReason})", sw.ElapsedMilliseconds, isError: true);
      return false;
    }

    LogStep(order.Id, 2, "Stock", "OK", sw.ElapsedMilliseconds);

    // Step3: Pricing the order
    sw.Restart();
    try
    {
      await _pricingService.CalculateTotalAsync(order, cancellationToken);
      sw.Stop();
      LogStep(order.Id, 3, "Pricing", $"OK (${order.TotalAmount:F2})", sw.ElapsedMilliseconds);
    }
    catch (InvalidCouponException ex)
    {
      sw.Stop();
      if (stockReserved)
      {
        await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        stockReserved = false;
      }

      order.Status = OrderStatus.Rejected;
      order.FailureReason = ex.Message;
      LogStep(order.Id, 3, "Pricing", $"REJECTED ({ex.Message})", sw.ElapsedMilliseconds, isError: true);
      await _orderRepository.SaveAsync(order, cancellationToken);
      return false;
    }

    // Step4: Payment with 3s timeout
    sw.Restart();
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

    PaymentResult paymentResult;
    try
    {
      paymentResult = await _paymentGateway.ProcessPaymentAsync(order.Id, order.TotalAmount, linkedCts.Token);
    }
    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
    {
      sw.Stop();
      if (stockReserved)
      {
        await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        stockReserved = false;
      }

      order.Status = OrderStatus.PaymentFailed;
      order.FailureReason = "Payment gateway timeout (3s)";
      LogStep(order.Id, 4, "Payment", "FAILED (Payment gateway timeout (3s))", sw.ElapsedMilliseconds, isError: true);
      await _orderRepository.SaveAsync(order, CancellationToken.None);
      return false;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      sw.Stop();
      if (stockReserved)
      {
        await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        stockReserved = false;
      }

      LogStep(order.Id, 4, "Payment", "CANCELLED", sw.ElapsedMilliseconds, isError: true);
      throw;
    }

    if (!paymentResult.Success || paymentResult.Status == PaymentStatus.Declined)
    {
      sw.Stop();
      if (stockReserved)
      {
        await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        stockReserved = false;
      }

      order.Status = OrderStatus.PaymentFailed;
      order.FailureReason = "Card declined by issuer";
      LogStep(order.Id, 4, "Payment", "DECLINED (Card declined by issuer)", sw.ElapsedMilliseconds, isError: true);
      await _orderRepository.SaveAsync(order, cancellationToken);
      return false;
    }

    sw.Stop();
    order.TransactionId = paymentResult.TransactionId;
    LogStep(order.Id, 4, "Payment", $"OK ({order.TransactionId})", sw.ElapsedMilliseconds);

    // Step5: Save with compensation on failure
    sw.Restart();
    order.TransactionId = paymentResult.TransactionId;
    order.Status = OrderStatus.Completed;
    try
    {
      await _orderRepository.SaveAsync(order, cancellationToken);
      orderSaved = true;
      sw.Stop();
      LogStep(order.Id, 5, "Save", "OK", sw.ElapsedMilliseconds);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception)
    {
      sw.Stop();

      // Compensation sequence:
      // 1. Refund payment
      if (!string.IsNullOrEmpty(order.TransactionId))
      {
        await _paymentGateway.RefundAsync(order.TransactionId, CancellationToken.None);
      }

      // 2. Release reserved stock
      if (stockReserved)
      {
        await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
        stockReserved = false;
      }

      // 3. Mark status as Failed
      order.Status = OrderStatus.Failed;

      // 4. Set compensation failure reason
      order.FailureReason = $"Database save failed after payment; payment {order.TransactionId} refunded";

      LogStep(order.Id, 5, "Save", $"FAILED ({order.FailureReason})", sw.ElapsedMilliseconds, isError: true);

      // 5. Persist final status to ensure all 20 records remain in orders.json
      try
      {
        await _orderRepository.SaveAsync(order, CancellationToken.None);
      }
      catch (Exception saveEx)
      {
        Logger.Error<OrderProcessor>($"Failed to persist compensated order #{order.Id}: {saveEx.Message}");
      }

      return false;
    }

    // Step6: Confirmation Email with bounded retry
    sw.Restart();
    bool emailSent = await SendConfirmationWithRetryAsync(order, customer, cancellationToken);
    sw.Stop();

    if (emailSent)
    {
      LogStep(order.Id, 6, "Email", "OK", sw.ElapsedMilliseconds);
    }
    else
    {
      LogStep(order.Id, 6, "Email", "FAILED (Max retries exceeded; queued to email_retry.json)", sw.ElapsedMilliseconds, isError: true);
    }

    // Return true only if Status == Completed
    return order.Status == OrderStatus.Completed;
    }
    catch (OperationCanceledException)
    {
      if (!orderSaved)
      {
        if (!string.IsNullOrEmpty(order.TransactionId))
        {
          await _paymentGateway.RefundAsync(order.TransactionId, CancellationToken.None);
        }

        if (stockReserved)
        {
          await _inventoryService.ReleaseStockAsync(order, CancellationToken.None);
          stockReserved = false;
        }

        order.Status = OrderStatus.Cancelled;
        order.FailureReason = "Order processing was cancelled";

        try
        {
          await _orderRepository.SaveAsync(order, CancellationToken.None);
        }
        catch (Exception saveEx)
        {
          Logger.Error<OrderProcessor>($"Failed to persist cancelled order #{order.Id}: {saveEx.Message}");
        }
      }

      throw;
    }
  }

  private async Task<bool> SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken cancellationToken)
  {
    const int maxAttempts = 4;
    int attempt = 0;

    while (attempt < maxAttempts)
    {
      attempt++;
      try
      {
        await _emailService.SendConfirmationAsync(order, customer, cancellationToken);
        return true;
      }
      catch (SmtpUnavailableException ex)
      {
        Logger.Error<OrderProcessor>($"Email attempt #{attempt} for Order #{order.Id} failed: {ex.Message}");

        if (attempt >= maxAttempts)
        {
          await RecordEmailRetryAsync(order, customer, attempt, ex.Message, cancellationToken);
          return false;
        }

        await Task.Delay(100 * attempt, cancellationToken);
      }
    }

    return false;
  }

  private async Task RecordEmailRetryAsync(Order order, Customer customer, int attempts, string lastError, CancellationToken cancellationToken)
  {
    try
    {
      var retryMessage = new EmailMessage
      {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        To = customer.Email,
        Subject = $"Order Confirmation #{order.Id}",
        Body = $"Dear {customer.Name}, your order #{order.Id} with total amount ${order.TotalAmount:F2} has been confirmed.",
        SentAt = DateTime.Now,
        Attempts = attempts,
        LastError = lastError
      };

      var retries = await _jsonFileStore.ReadAsync<List<EmailMessage>>(DataFiles.EmailRetry, cancellationToken);
      retries ??= [];
      retries.Add(retryMessage);
      await _jsonFileStore.WriteAsync(DataFiles.EmailRetry, retries, cancellationToken);
      Logger.Error<OrderProcessor>($"Order #{order.Id} added to email retry queue after {attempts} failed attempts.");
      Logger.Error<CheckoutService>($"Email for Order #{order.Id} queued for retry later");
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      Logger.Error<OrderProcessor>($"Failed to record email retry for Order #{order.Id}: {ex.Message}");
    }
  }

  private static void LogStep(int orderId, int step, string stepName, string result, long elapsedMs, bool isError = false)
  {
    var color = isError ? ConsoleColor.Red : ConsoleColor.Green;
    lock (_logLock)
    {
      try
      {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Environment.CurrentManagedThreadId}] Order #{orderId} -> Step {step}/6 {stepName} ... {result} in {elapsedMs} ms");
      }
      finally
      {
        Console.ResetColor();
      }
    }
  }
}
