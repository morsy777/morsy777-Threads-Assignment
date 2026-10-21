using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Models.Results;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;
using OrderProcessing.Validators;

namespace OrderProcessing.Services;

public class CheckoutService
{
  private readonly CustomerService _customerService;
  private readonly ProductRepository _productRepository;
  private readonly OrderPricer _orderPricer;
  private readonly OrderValidator _orderValidator;
  private readonly IPaymentGateway _paymentGateway;
  private readonly OrderRepository _orderRepository;
  private readonly InvoiceGenerator _invoiceGenerator;
  private readonly IEmailService _emailService;
  private readonly JsonFileStore _jsonFileStore;

  public CheckoutService(
    CustomerService? customerService = null,
    ProductRepository? productRepository = null,
    OrderPricer? orderPricer = null,
    OrderValidator? orderValidator = null,
    IPaymentGateway? paymentGateway = null,
    OrderRepository? orderRepository = null,
    InvoiceGenerator? invoiceGenerator = null,
    IEmailService? emailService = null)
  {
    _customerService = customerService ?? new CustomerService();
    _productRepository = productRepository ?? new ProductRepository();
    _orderPricer = orderPricer ?? new OrderPricer([7]);
    _orderValidator = orderValidator ?? new OrderValidator();
    _paymentGateway = paymentGateway ?? new PaymentGateway();
    _orderRepository = orderRepository ?? new OrderRepository();
    _invoiceGenerator = invoiceGenerator ?? new InvoiceGenerator();
    _emailService = emailService ?? new EmailService();
    _jsonFileStore = new JsonFileStore();
  }

  public async Task<bool> CheckoutAsync(Order order, CancellationToken cancellationToken = default)
  {
    // Step 1: Customer check
    // Scenario 1: Order #15 customer 99 not found -> throws CustomerNotFoundException
    var customer = await _customerService.GetCustomerAsync(order.CustomerId, cancellationToken);

    // Step 2: Validate order
    await _orderValidator.ValidateOrderAsync(order, cancellationToken);
    if (order.Status == OrderStatus.Rejected)
    {
      await _orderRepository.SaveAsync(order, cancellationToken);
      return false;
    }

    // Step 3: Stock reservation
    var reservedItems = new List<OrderItem>();
    foreach (var item in order.Items)
    {
      var reserved = await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);
      if (reserved)
      {
        reservedItems.Add(item);
        continue;
      }

      foreach (var reservedItem in reservedItems)
      {
        await _productRepository.ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity, CancellationToken.None);
      }

      var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
      order.Status = OrderStatus.Rejected;
      order.FailureReason = $"Insufficient stock for '{product?.Name}'";
      await _orderRepository.SaveAsync(order, cancellationToken);
      return false;
    }

    // Step 4: Pricing
    await _orderPricer.PriceOrderAsync(order, cancellationToken);

    // Step 5: Fraud Check
    await PerformFraudCheckAsync(order, customer, cancellationToken);

    // Step 6: Payment capture with 2-second timeout
    using var paymentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    paymentCts.CancelAfter(TimeSpan.FromSeconds(2));

    PaymentResult paymentResult;
    try
    {
      paymentResult = await _paymentGateway.ProcessPaymentAsync(order.Id, order.TotalAmount, paymentCts.Token);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && paymentCts.IsCancellationRequested)
    {
      // Scenario 2: Order #5 timeout
      foreach (var item in order.Items)
      {
        await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
      }

      order.Status = OrderStatus.PaymentFailed;
      order.FailureReason = "Payment processing timed out";
      await _orderRepository.SaveAsync(order, CancellationToken.None);

      Logger.Error<CheckoutService>($"Payment timed out for Order #{order.Id}.");
      throw new TimeoutException($"Payment timed out for Order #{order.Id}");
    }

    // Scenario 3: Order #8 payment declined
    if (!paymentResult.Success || paymentResult.Status == PaymentStatus.Declined)
    {
      foreach (var item in order.Items)
      {
        await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
      }

      order.Status = OrderStatus.PaymentFailed;
      order.FailureReason = paymentResult.Message;
      await _orderRepository.SaveAsync(order, cancellationToken);

      Logger.Error<CheckoutService>($"Payment declined for Order #{order.Id}: {paymentResult.Message}");
      return false;
    }

    order.TransactionId = paymentResult.TransactionId;

    // Step 7: Save order as Completed
    // Scenario 4: Order #13 fails save after payment
    order.Status = OrderStatus.Completed;
    try
    {
      await _orderRepository.SaveAsync(order, cancellationToken);
    }
    catch (DatabaseWriteException)
    {
      if (!string.IsNullOrEmpty(order.TransactionId))
      {
        await _paymentGateway.RefundAsync(order.TransactionId);
      }

      foreach (var item in order.Items)
      {
        await _productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
      }

      Logger.Error<CheckoutService>($"Database save failed after payment for Order #{order.Id}. Compensating refund initiated.");
      throw;
    }

    // Step 8: Invoice generation
    await _invoiceGenerator.GenerateAsync(order, cancellationToken);

    // Step 9: Email confirmation with bounded retry
    // Scenarios 5 & 6: Order #3 and Order #17
    await SendConfirmationWithRetryAsync(order, customer, cancellationToken);

    return true;
  }

  private static async Task PerformFraudCheckAsync(Order order, Customer customer, CancellationToken cancellationToken)
  {
    await Task.Delay(150, cancellationToken);
    Logger.Info<CheckoutService>($"Fraud check passed for Order #{order.Id} (Customer: {customer.Name}).");
  }

  private async Task SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken cancellationToken)
  {
    const int maxAttempts = 3;
    int attempt = 0;

    while (attempt < maxAttempts)
    {
      attempt++;
      try
      {
        await _emailService.SendConfirmationAsync(order, customer, cancellationToken);
        return;
      }
      catch (SmtpUnavailableException ex)
      {
        Logger.Error<CheckoutService>($"Email attempt #{attempt} for Order #{order.Id} failed: {ex.Message}");

        if (attempt >= maxAttempts)
        {
          await RecordEmailRetryAsync(order, customer, attempt, ex.Message, cancellationToken);
          throw;
        }

        await Task.Delay(100 * attempt, cancellationToken);
      }
    }
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
      Logger.Error<CheckoutService>($"Order #{order.Id} added to email retry queue after {attempts} failed attempts.");
    }
    catch (Exception ex)
    {
      Logger.Error<CheckoutService>($"Failed to record email retry for Order #{order.Id}: {ex.Message}");
    }
  }
}
