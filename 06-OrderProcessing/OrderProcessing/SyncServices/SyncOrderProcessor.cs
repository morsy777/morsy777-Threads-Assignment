using System.Diagnostics;
using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Models.Results;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Services;
using OrderProcessing.SyncRepositories;

namespace OrderProcessing.SyncServices;

public class SyncOrderProcessor
{
  private static readonly Lock _logLock = new();
  private readonly SyncCustomerService _customerService;
  private readonly SyncInventoryService _inventoryService;
  private readonly SyncPricingService _pricingService;
  private readonly ISyncPaymentGateway _paymentGateway;
  private readonly SyncOrderRepository _orderRepository;
  private readonly ISyncEmailService _emailService;
  private readonly JsonFileStore _jsonFileStore;

  public SyncOrderProcessor() : this(null, null, null, null)
  {
  }

  public SyncOrderProcessor(Dictionary<int, int>? emailConfig,
      IEnumerable<int>? timeoutOrderIds,
      IEnumerable<int>? declineOrderIds,
      IEnumerable<int>? failSaveForOrderIds)
  {
      _customerService = new SyncCustomerService();
      _inventoryService = new SyncInventoryService();
      _pricingService = new SyncPricingService(new SyncOrderPricer([]));
      _paymentGateway = new SyncPaymentGateway(timeoutOrderIds ?? [], declineOrderIds ?? []);
      _orderRepository = new SyncOrderRepository(failSaveForOrderIds ?? []);
      _emailService = new SyncEmailService(emailConfig ?? []);
      _jsonFileStore = new JsonFileStore();
  }

  public bool ProcessOrder(Order order)
  {
    if (order is null)
      return false;

    bool stockReserved = false;
    var sw = Stopwatch.StartNew();

    // Step1: Customer check
    Customer customer;
    sw.Restart();
    try
    {
      customer = _customerService.GetCustomer(order.CustomerId);
    }
    catch (CustomerNotFoundException ex)
    {
      sw.Stop();
      order.Status = OrderStatus.Rejected;
      order.FailureReason = ex.Message;
      LogStep(order.Id, 1, "Customer", $"REJECTED ({ex.Message})", sw.ElapsedMilliseconds, isError: true);
      _orderRepository.Save(order);
      return false;
    }

    if (!customer.IsActive)
    {
      sw.Stop();
      order.Status = OrderStatus.Rejected;
      order.FailureReason = "Customer is inactive";
      LogStep(order.Id, 1, "Customer", "REJECTED (Customer is inactive)", sw.ElapsedMilliseconds, isError: true);
      _orderRepository.Save(order);
      return false;
    }

    sw.Stop();
    LogStep(order.Id, 1, "Customer", $"OK ({customer.Name})", sw.ElapsedMilliseconds);

    // Step2: Stock Reservation
    sw.Restart();
    stockReserved = _inventoryService.ReserveStock(order);
    sw.Stop();

    if (!stockReserved)
    {
      if (order.Status != OrderStatus.Rejected)
      {
        order.Status = OrderStatus.Rejected;
        order.FailureReason ??= "Insufficient stock";
        _orderRepository.Save(order);
      }
      LogStep(order.Id, 2, "Stock", $"REJECTED ({order.FailureReason})", sw.ElapsedMilliseconds, isError: true);
      return false;
    }

    LogStep(order.Id, 2, "Stock", "OK", sw.ElapsedMilliseconds);

    // Step3: Pricing the order
    sw.Restart();
    try
    {
      _pricingService.CalculateTotal(order);
      sw.Stop();
      LogStep(order.Id, 3, "Pricing", $"OK (${order.TotalAmount:F2})", sw.ElapsedMilliseconds);
    }
    catch (InvalidCouponException ex)
    {
      sw.Stop();
      if (stockReserved)
      {
        _inventoryService.ReleaseStock(order);
        stockReserved = false;
      }

      order.Status = OrderStatus.Rejected;
      order.FailureReason = ex.Message;
      LogStep(order.Id, 3, "Pricing", $"REJECTED ({ex.Message})", sw.ElapsedMilliseconds, isError: true);
      _orderRepository.Save(order);
      return false;
    }

    // Step4: Payment with 3s timeout
    sw.Restart();
    PaymentResult? paymentResult = null;
    Exception? paymentException = null;

    var paymentThread = new Thread(() =>
    {
      try
      {
        paymentResult = _paymentGateway.ProcessPayment(order.Id, order.TotalAmount);
      }
      catch (Exception ex)
      {
        paymentException = ex;
      }
    });

    paymentThread.Start();
    bool completedInTime = paymentThread.Join(TimeSpan.FromSeconds(3));

    if (!completedInTime || paymentException is TimeoutException or ThreadInterruptedException)
    {
      paymentThread.Interrupt();
      sw.Stop();
      if (stockReserved)
      {
        _inventoryService.ReleaseStock(order);
        stockReserved = false;
      }

      order.Status = OrderStatus.PaymentFailed;
      order.FailureReason = "Payment gateway timeout (3s)";
      LogStep(order.Id, 4, "Payment", "FAILED (Payment gateway timeout (3s))", sw.ElapsedMilliseconds, isError: true);
      _orderRepository.Save(order);
      return false;
    }

    if (paymentException is not null)
    {
      sw.Stop();
      if (stockReserved)
      {
        _inventoryService.ReleaseStock(order);
        stockReserved = false;
      }
      throw paymentException;
    }

    if (paymentResult is null || !paymentResult.Success || paymentResult.Status == PaymentStatus.Declined)
    {
      sw.Stop();
      if (stockReserved)
      {
        _inventoryService.ReleaseStock(order);
        stockReserved = false;
      }

      order.Status = OrderStatus.PaymentFailed;
      order.FailureReason = "Card declined by issuer";
      LogStep(order.Id, 4, "Payment", "DECLINED (Card declined by issuer)", sw.ElapsedMilliseconds, isError: true);
      _orderRepository.Save(order);
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
      _orderRepository.Save(order);
      sw.Stop();
      LogStep(order.Id, 5, "Save", "OK", sw.ElapsedMilliseconds);
    }
    catch (Exception)
    {
      sw.Stop();

      // Compensation sequence:
      // 1. Refund payment
      if (!string.IsNullOrEmpty(order.TransactionId))
      {
        _paymentGateway.Refund(order.TransactionId);
      }

      // 2. Release reserved stock
      if (stockReserved)
      {
        _inventoryService.ReleaseStock(order);
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
        _orderRepository.Save(order);
      }
      catch (Exception saveEx)
      {
        Logger.Error<SyncOrderProcessor>($"Failed to persist compensated order #{order.Id}: {saveEx.Message}");
      }

      return false;
    }

    // Step6: Confirmation Email with bounded retry
    sw.Restart();
    bool emailSent = SendConfirmationWithRetry(order, customer);
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

  private bool SendConfirmationWithRetry(Order order, Customer customer)
  {
    const int maxAttempts = 4;
    int attempt = 0;

    while (attempt < maxAttempts)
    {
      attempt++;
      try
      {
        _emailService.SendConfirmation(order, customer);
        return true;
      }
      catch (SmtpUnavailableException ex)
      {
        Logger.Error<SyncOrderProcessor>($"Email attempt #{attempt} for Order #{order.Id} failed: {ex.Message}");

        if (attempt >= maxAttempts)
        {
          RecordEmailRetry(order, customer, attempt, ex.Message);
          return false;
        }

        Thread.Sleep(100 * attempt);
      }
    }

    return false;
  }

  private void RecordEmailRetry(Order order, Customer customer, int attempts, string lastError)
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

      var retries = _jsonFileStore.Read<List<EmailMessage>>(DataFiles.EmailRetry);
      retries ??= [];
      retries.Add(retryMessage);
      _jsonFileStore.Write(DataFiles.EmailRetry, retries);
      Logger.Error<SyncOrderProcessor>($"Order #{order.Id} added to email retry queue after {attempts} failed attempts.");
      Logger.Error<CheckoutService>($"Email for Order #{order.Id} queued for retry later");
    }
    catch (Exception ex)
    {
      Logger.Error<SyncOrderProcessor>($"Failed to record email retry for Order #{order.Id}: {ex.Message}");
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
