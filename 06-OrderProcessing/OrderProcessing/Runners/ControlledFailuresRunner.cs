using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;
using OrderProcessing.Services;

namespace OrderProcessing.Runners;

public class ControlledFailuresRunner
{
  private readonly CheckoutService _checkoutService;
  private readonly EmailService _emailService;
  private readonly IncomingOrderRepository _incomingOrderRepository;

  public ControlledFailuresRunner(
    CheckoutService checkoutService,
    EmailService emailService,
    IncomingOrderRepository? incomingOrderRepository = null)
  {
    _checkoutService = checkoutService;
    _emailService = emailService;
    _incomingOrderRepository = incomingOrderRepository ?? new IncomingOrderRepository();
  }

  public async Task RunAllScenariosAsync(CancellationToken cancellationToken = default)
  {
    Console.WriteLine();
    Console.WriteLine("==========================================================================");
    Console.WriteLine("           Task 9.2 — Controlled Failures Scenario Runner                 ");
    Console.WriteLine("==========================================================================");

    int passedCount = 0;
    int totalCount = 7;

    if (await RunScenario1_CustomerNotFoundAsync(cancellationToken)) passedCount++;
    if (await RunScenario2_PaymentTimeoutAsync(cancellationToken)) passedCount++;
    if (await RunScenario3_PaymentDeclinedAsync(cancellationToken)) passedCount++;
    if (await RunScenario4_DatabaseSaveFailsAsync(cancellationToken)) passedCount++;
    if (await RunScenario5_EmailFailsTwiceThenSucceedsAsync(cancellationToken)) passedCount++;
    if (await RunScenario6_EmailAlwaysFailsAsync(cancellationToken)) passedCount++;
    if (await RunScenario_UnaffectedOrderAsync(cancellationToken)) passedCount++;

    Console.WriteLine();
    Console.WriteLine("==========================================================================");
    Console.WriteLine($" Summary: {passedCount}/{totalCount} Scenarios Passed");
    Console.WriteLine("==========================================================================");
  }

  private async Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken)
  {
    var orders = await _incomingOrderRepository.GetAllAsync(cancellationToken);
    foreach (var o in orders)
    {
      if (o.Id == id)
        return o;
    }
    return null;
  }

  private async Task<bool> RunScenario1_CustomerNotFoundAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Scenario 1: Order #15 — Customer Not Found ---");
    var order = await GetOrderByIdAsync(15, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #15 not found in incoming orders.");
      return false;
    }

    Console.WriteLine($"Order #15 references CustomerId: {order.CustomerId}");
    Console.WriteLine("Expected: CustomerNotFoundException with message 'Customer #99 not found'");

    try
    {
      await _checkoutService.CheckoutAsync(order, cancellationToken);
      Console.WriteLine("Actual: Checkout completed without exception (UNEXPECTED).");
      Console.WriteLine("Result: FAILED");
      return false;
    }
    catch (CustomerNotFoundException ex)
    {
      Console.WriteLine($"Actual: Caught CustomerNotFoundException — '{ex.Message}'");
      bool matches = ex.Message.Contains("Customer #99 not found", StringComparison.OrdinalIgnoreCase);
      if (matches)
      {
        Console.WriteLine("Result: PASSED");
        return true;
      }
      else
      {
        Console.WriteLine("Result: FAILED (Exception message did not match expected 'Customer #99 not found')");
        return false;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }

  private async Task<bool> RunScenario2_PaymentTimeoutAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Scenario 2: Order #5 — Payment Timeout ---");
    var order = await GetOrderByIdAsync(5, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #5 not found in incoming orders.");
      return false;
    }

    Console.WriteLine("Expected: TimeoutException (payment delayed 5000ms exceeding 2000ms timeout)");

    try
    {
      await _checkoutService.CheckoutAsync(order, cancellationToken);
      Console.WriteLine("Actual: Checkout completed without exception (UNEXPECTED).");
      Console.WriteLine("Result: FAILED");
      return false;
    }
    catch (TimeoutException ex)
    {
      Console.WriteLine($"Actual: Caught TimeoutException — '{ex.Message}'");
      Console.WriteLine($"Order #5 Status: {order.Status}, FailureReason: {order.FailureReason}");
      bool passed = order.Status == OrderStatus.PaymentFailed;
      Console.WriteLine($"Result: {(passed ? "PASSED" : "FAILED")}");
      return passed;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }

  private async Task<bool> RunScenario3_PaymentDeclinedAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Scenario 3: Order #8 — Payment Declined ---");
    var order = await GetOrderByIdAsync(8, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #8 not found in incoming orders.");
      return false;
    }

    Console.WriteLine("Expected: Checkout returns false, Status == PaymentFailed, Message == 'Card declined by issuer'");

    try
    {
      bool success = await _checkoutService.CheckoutAsync(order, cancellationToken);
      Console.WriteLine($"Actual: Checkout returned {success}, Status == {order.Status}, Message == '{order.FailureReason}'");

      bool passed = !success &&
                    order.Status == OrderStatus.PaymentFailed &&
                    string.Equals(order.FailureReason, "Card declined by issuer", StringComparison.OrdinalIgnoreCase);

      Console.WriteLine($"Result: {(passed ? "PASSED" : "FAILED")}");
      return passed;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }

  private async Task<bool> RunScenario4_DatabaseSaveFailsAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Scenario 4: Order #13 — Database Save Fails After Payment ---");
    var order = await GetOrderByIdAsync(13, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #13 not found in incoming orders.");
      return false;
    }

    Console.WriteLine("Expected: DatabaseWriteException with message 'Connection lost during commit' when saving Status == Completed");

    try
    {
      await _checkoutService.CheckoutAsync(order, cancellationToken);
      Console.WriteLine("Actual: Checkout completed without exception (UNEXPECTED).");
      Console.WriteLine("Result: FAILED");
      return false;
    }
    catch (DatabaseWriteException ex)
    {
      Console.WriteLine($"Actual: Caught DatabaseWriteException — '{ex.Message}'");
      bool matches = ex.Message.Contains("Connection lost during commit", StringComparison.OrdinalIgnoreCase);
      Console.WriteLine($"Result: {(matches ? "PASSED" : "FAILED")}");
      return matches;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }

  private async Task<bool> RunScenario5_EmailFailsTwiceThenSucceedsAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Scenario 5: Order #3 — Email Fails Twice, Then Succeeds ---");
    var order = await GetOrderByIdAsync(3, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #3 not found in incoming orders.");
      return false;
    }

    Console.WriteLine("Expected: Email attempts 1 & 2 throw SmtpUnavailableException, attempt 3 succeeds, Order Completed");

    try
    {
      bool success = await _checkoutService.CheckoutAsync(order, cancellationToken);
      int attemptCount = _emailService.GetAttemptCount(3);
      Console.WriteLine($"Actual: Checkout returned {success}, Status == {order.Status}, Email attempts == {attemptCount}");

      bool passed = success && order.Status == OrderStatus.Completed && attemptCount == 3;
      Console.WriteLine($"Result: {(passed ? "PASSED" : "FAILED")}");
      return passed;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }

  private async Task<bool> RunScenario6_EmailAlwaysFailsAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Scenario 6: Order #17 — Email Always Fails (Bounded Retries) ---");
    var order = await GetOrderByIdAsync(17, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #17 not found in incoming orders.");
      return false;
    }

    Console.WriteLine("Expected: SmtpUnavailableException thrown on all attempts, retries stopped after 3 attempts");

    try
    {
      await _checkoutService.CheckoutAsync(order, cancellationToken);
      Console.WriteLine("Actual: Checkout completed without exception (UNEXPECTED).");
      Console.WriteLine("Result: FAILED");
      return false;
    }
    catch (SmtpUnavailableException ex)
    {
      int attemptCount = _emailService.GetAttemptCount(17);
      Console.WriteLine($"Actual: Caught SmtpUnavailableException — '{ex.Message}', Total attempts == {attemptCount}");

      bool passed = attemptCount == 3;
      Console.WriteLine($"Result: {(passed ? "PASSED (Retries bounded at 3)" : "FAILED (Unexpected attempt count)")}");
      return passed;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }

  private async Task<bool> RunScenario_UnaffectedOrderAsync(CancellationToken cancellationToken)
  {
    Console.WriteLine("\n--- Verification: Normal Order #1 — Unaffected ---");
    var order = await GetOrderByIdAsync(1, cancellationToken);
    if (order is null)
    {
      Console.WriteLine("FAIL: Order #1 not found in incoming orders.");
      return false;
    }

    Console.WriteLine("Expected: Order #1 completes successfully, Status == Completed, Email attempt == 1");

    try
    {
      bool success = await _checkoutService.CheckoutAsync(order, cancellationToken);
      int attemptCount = _emailService.GetAttemptCount(1);
      Console.WriteLine($"Actual: Checkout returned {success}, Status == {order.Status}, Email attempts == {attemptCount}");

      bool passed = success && order.Status == OrderStatus.Completed && attemptCount == 1;
      Console.WriteLine($"Result: {(passed ? "PASSED" : "FAILED")}");
      return passed;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Actual: Caught unexpected exception {ex.GetType().Name} — '{ex.Message}'");
      Console.WriteLine("Result: FAILED");
      return false;
    }
  }
}
