using System.Diagnostics;
using System.Globalization;
using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;
using OrderProcessing.Services;

namespace OrderProcessing.Runners;

public class Phase4Runner
{
    private readonly JsonFileStore _jsonFileStore;
    private readonly DataSeeder _dataSeeder;
    private readonly IncomingOrderRepository _incomingOrderRepository;
    private OrderProcessor _orderProcessor;
    private CustomerDashboardService _customerDashboardService;

    public Phase4Runner()
    {
        _jsonFileStore = new JsonFileStore();
        _dataSeeder = new DataSeeder(_jsonFileStore);
        _incomingOrderRepository = new IncomingOrderRepository();
        _orderProcessor = CreateConfiguredOrderProcessor();
        _customerDashboardService = new CustomerDashboardService(failPaymentsForCustomerIds: [99]);
    }
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
      await RunLiveCheckoutAsync(cancellationToken);
      await RunCustomerDashboardAsync(cancellationToken);
      await RunFastestShippingQuoteAsync(cancellationToken);
      await RunParallelProcessingAsync(cancellationToken);
    }

    public async Task RunFastestShippingQuoteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        Console.WriteLine("==========================================================================");
        Console.WriteLine("          Task 10.3 — Fastest Shipping Quote (Phase 4)                    ");
        Console.WriteLine("==========================================================================");

        var shippingService = new ShippingQuoteService();
        var incomingOrders = await _incomingOrderRepository.GetAllAsync(cancellationToken);

        for (int orderId = 1; orderId <= 5; orderId++)
        {
            Order? order = null;
            foreach (var o in incomingOrders)
            {
                if (o.Id == orderId)
                {
                    order = o;
                    break;
                }
            }

            if (order == null)
            {
                Console.WriteLine($"Order #{orderId} not found.");
                continue;
            }

            Console.WriteLine($"--- Order #{order.Id} ---");
            var quote = await shippingService.GetFastestQuoteAsync(order, cancellationToken);

            string cancelledStr;
            if (quote.CancelledCarriers.Count > 0)
            {
                cancelledStr = string.Join(", ", quote.CancelledCarriers);
            }
            else
            {
                cancelledStr = "None";
            }

            Console.WriteLine($"Winning carrier:    {quote.Carrier}");
            Console.WriteLine($"Price:              ${quote.Price:F2}");
            Console.WriteLine($"Elapsed time:       {quote.ElapsedMs} ms");
            Console.WriteLine($"Cancelled carriers: {cancelledStr}");
            Console.WriteLine();
        }

        Console.WriteLine("--- Order #6 (All carriers configured to fail) ---");
        Order? order6 = null;
        foreach (var o in incomingOrders)
        {
            if (o.Id == 6)
            {
                order6 = o;
                break;
            }
        }

        if (order6 == null)
        {
            Console.WriteLine("Order #6 not found.");
            return;
        }

        var failingShippingService = new ShippingQuoteService(failForOrderIds: [6]);

        try
        {
            await failingShippingService.GetFastestQuoteAsync(order6, cancellationToken);
            Console.WriteLine("FAILED: Expected ShippingProviderUnavailableException was not thrown.");
        }
        catch (ShippingProviderUnavailableException ex)
        {
            Console.WriteLine($"Caught expected exception: {ex.GetType().Name}(\"{ex.Message}\")");
        }
    }

    public async Task RunCustomerDashboardAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        Console.WriteLine("========== Customer Dashboard ==========");

        try
        {
            var dashboard = await _customerDashboardService.LoadAsync(3, cancellationToken);

            Console.Write($"Customer #{dashboard.Customer.Id} {dashboard.Customer.Name} — ");

            Console.Write($"{dashboard.Orders.Count} orders (");
            for (int i = 0; i < dashboard.Orders.Count; i++)
            {
                if (i > 0)
                    Console.Write(", ");

                var order = dashboard.Orders[i];
                Console.Write($"#{order.Id} {order.Status}");
            }

            Console.Write("), ");

            Console.Write($"{dashboard.Payments.Count} payments (");
            for (int i = 0; i < dashboard.Payments.Count; i++)
            {
                if (i > 0)
                    Console.Write(", ");

                var payment = dashboard.Payments[i];
                Console.Write($"#{payment.OrderId} {payment.Status}");
            }

            Console.Write("), ");

            Console.Write($"{dashboard.Emails.Count} email");
            if (dashboard.Emails.Count != 1)
                Console.Write("s");

            Console.Write(" (");
            for (int i = 0; i < dashboard.Emails.Count; i++)
            {
                if (i > 0)
                    Console.Write(", ");

                Console.Write($"#{dashboard.Emails[i].OrderId}");
            }

            Console.WriteLine(")");

            Console.WriteLine(
                $"Load times: Profile={_customerDashboardService.CustomerLoadMs} ms, " +
                $"Orders={_customerDashboardService.OrdersLoadMs} ms, " +
                $"Payments={_customerDashboardService.PaymentsLoadMs} ms, " +
                $"Emails={_customerDashboardService.EmailsLoadMs} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Customer #3: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("========== Customer #99 Failure Test ==========");

        try
        {
            await _customerDashboardService.LoadAsync(99, cancellationToken);
        }
        catch (AggregateException ex)
        {
            foreach (var innerException in ex.Flatten().InnerExceptions)
            {
                Console.WriteLine(innerException.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }


    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
      await _dataSeeder.SeedAsync(cancellationToken);
      _orderProcessor = CreateConfiguredOrderProcessor();
    }

    public async Task RunLiveCheckoutAsync(CancellationToken cancellationToken = default)
    {
        // 1. Clean reset before processing
        await ResetAsync(cancellationToken);

        // 2. Load all 20 incoming orders
        var incomingOrders = await _incomingOrderRepository.GetAllAsync(cancellationToken);

        Console.WriteLine();
        Console.WriteLine("==========================================================================");
        Console.WriteLine("             Task 10.1 — Live Checkout Progress (Phase 4)                 ");
        Console.WriteLine("==========================================================================");

        // 3. Start all incoming orders concurrently without awaiting each individually
        var pendingTasks = new List<Task<OrderExecutionResult>>(incomingOrders.Count);
        foreach (var order in incomingOrders)
        {
            pendingTasks.Add(ProcessSingleOrderAsync(order, cancellationToken));
        }

        int totalOrders = pendingTasks.Count;
        int completedCounter = 0;
        var completedResults = new List<OrderExecutionResult>(totalOrders);

        // 4. Report completed orders immediately as each finishes using Task.WhenAny
        while (pendingTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(pendingTasks);

            // Swap with last and remove at end to avoid shifting or list rebuilding
            int index = pendingTasks.IndexOf(completedTask);
            if (index >= 0)
            {
                pendingTasks[index] = pendingTasks[pendingTasks.Count - 1];
                pendingTasks.RemoveAt(pendingTasks.Count - 1);
            }

            completedCounter++;
            var result = await completedTask;
            completedResults.Add(result);

            PrintProgressLine(completedCounter, totalOrders, result);
        }

        // 5. Print final status counts matching Task 9.4
        PrintStatusSummary(completedResults);
    }

    private async Task<OrderExecutionResult> ProcessSingleOrderAsync(Order order, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _orderProcessor.ProcessOrderAsync(order, cancellationToken);
        }
        catch (Exception ex)
        {
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;
            }
        }
        finally
        {
            stopwatch.Stop();
        }

        return new OrderExecutionResult(order, stopwatch.Elapsed.TotalSeconds);
    }

    private static void PrintProgressLine(int counter, int total, OrderExecutionResult result)
    {
        string statusText;
        if (!string.IsNullOrWhiteSpace(result.Order.FailureReason))
        {
            statusText = $"{result.Order.Status} ({result.Order.FailureReason})";
        }
        else
        {
            statusText = $"{result.Order.Status}";
        }

        string elapsed = result.ElapsedSeconds.ToString("0.0", CultureInfo.InvariantCulture);
        Console.WriteLine($"[{counter,2}/{total}] Order #{result.Order.Id} -> {statusText} after {elapsed} s");
    }

    private static void PrintStatusSummary(List<OrderExecutionResult> results)
    {
        int completedCount = 0;
        int rejectedCount = 0;
        int paymentFailedCount = 0;
        int failedCount = 0;

        foreach (var res in results)
        {
            switch (res.Order.Status)
            {
                case OrderStatus.Completed:
                    completedCount++;
                    break;
                case OrderStatus.Rejected:
                    rejectedCount++;
                    break;
                case OrderStatus.PaymentFailed:
                    paymentFailedCount++;
                    break;
                case OrderStatus.Failed:
                    failedCount++;
                    break;
            }
        }

        Console.WriteLine();
        Console.WriteLine("=================== Status Counts ===================");
        Console.WriteLine($"Completed:      {completedCount}");
        Console.WriteLine($"Rejected:       {rejectedCount}");
        Console.WriteLine($"PaymentFailed:  {paymentFailedCount}");
        Console.WriteLine($"Failed:         {failedCount}");
        Console.WriteLine($"Total:          {results.Count}");
        Console.WriteLine("=====================================================");
    }

    private static OrderProcessor CreateConfiguredOrderProcessor()
    {
        // Exactly the same failure configuration as Task 9.2:
        // - Order #3: Email fails twice, then succeeds
        // - Order #17: Email always fails (int.MaxValue)
        // - Order #5: Payment timeout
        // - Order #8: Payment declined
        // - Order #13: Database save fails (compensation refund & stock release)
        return new OrderProcessor(emailConfig: new Dictionary<int, int>
            {
                [3] = 2,
                [17] = int.MaxValue
            },
            timeoutOrderIds: [5],
            declineOrderIds: [8],
            failSaveForOrderIds: [13]
        );
    }

    public async Task RunParallelProcessingAsync(CancellationToken cancellationToken = default)
    {
        RunCpuBoundFraudScoring(cancellationToken);
        await RunIoBoundEmailResendAsync(cancellationToken);
    }

    public void RunCpuBoundFraudScoring(CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        Console.WriteLine("==========================================================================");
        Console.WriteLine("       Task 10.4 — Part A: CPU-Bound Fraud Scoring (Parallel.For)         ");
        Console.WriteLine("==========================================================================");

        // 1. Create 10,000 in-memory orders (IDs 1 to 10,000)
        var orders = new List<Order>(10000);
        for (int i = 1; i <= 10000; i++)
        {
            orders.Add(new Order { Id = i });
        }

        // 2. Sequential run
        var sequentialFlagged = new List<int>();
        var sequentialSw = Stopwatch.StartNew();
        for (int i = 0; i < orders.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Score(orders[i].Id) >= 90)
            {
                sequentialFlagged.Add(orders[i].Id);
            }
        }
        sequentialSw.Stop();

        // 3. Parallel run using Parallel.For
        var parallelFlagged = new List<int>();
        var flaggedLock = new object();
        var distinctThreadIds = new HashSet<int>();
        var threadLock = new object();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        var parallelSw = Stopwatch.StartNew();
        try
        {
            Parallel.For(0, orders.Count, parallelOptions, i =>
            {
                int threadId = Environment.CurrentManagedThreadId;
                lock (threadLock)
                {
                    distinctThreadIds.Add(threadId);
                }

                int orderId = orders[i].Id;
                if (Score(orderId) >= 90)
                {
                    lock (flaggedLock)
                    {
                        parallelFlagged.Add(orderId);
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("CPU-bound fraud scoring was cancelled.");
            throw;
        }
        parallelSw.Stop();

        parallelFlagged.Sort();

        // 4. Verification
        bool countMatch = sequentialFlagged.Count == 1005 && parallelFlagged.Count == 1005;
        bool idsMatch = sequentialFlagged.Count == parallelFlagged.Count;
        if (idsMatch)
        {
            for (int i = 0; i < sequentialFlagged.Count; i++)
            {
                if (sequentialFlagged[i] != parallelFlagged[i])
                {
                    idsMatch = false;
                    break;
                }
            }
        }

        bool firstFiveMatch = sequentialFlagged.Count >= 5
            && sequentialFlagged[0] == 4
            && sequentialFlagged[1] == 8
            && sequentialFlagged[2] == 12
            && sequentialFlagged[3] == 29
            && sequentialFlagged[4] == 33;

        double speedup = parallelSw.ElapsedMilliseconds > 0
            ? (double)sequentialSw.ElapsedMilliseconds / parallelSw.ElapsedMilliseconds
            : 0;

        Console.WriteLine($"Processor Count:         {Environment.ProcessorCount}");
        Console.WriteLine($"Distinct Threads Used:   {distinctThreadIds.Count}");
        Console.WriteLine($"Sequential Execution:    {sequentialSw.ElapsedMilliseconds} ms (Flagged: {sequentialFlagged.Count})");
        Console.WriteLine($"Parallel Execution:      {parallelSw.ElapsedMilliseconds} ms (Flagged: {parallelFlagged.Count})");
        Console.WriteLine($"Speed-up Factor:         {speedup:F2}x");

        string firstFiveStr = "";
        for (int i = 0; i < Math.Min(5, parallelFlagged.Count); i++)
        {
            if (i > 0) firstFiveStr += ", ";
            firstFiveStr += parallelFlagged[i];
        }
        Console.WriteLine($"First Five Flagged IDs:  {firstFiveStr}");

        bool allPassed = countMatch && idsMatch && firstFiveMatch;
        Console.WriteLine($"Verification Result:     {(allPassed ? "PASS" : "FAIL")} (Expected 1005 flagged orders, first five: 4, 8, 12, 29, 33)");
    }

    private static int Score(int orderId)
    {
        long h = orderId;
        for (int i = 0; i < 100_000; i++)
            h = (h * 31 + i) % 1_000_000_007;
        return (int)(h % 100);
    }

  public async Task RunIoBoundEmailResendAsync(CancellationToken cancellationToken = default)
  {
    Console.WriteLine();
    Console.WriteLine("==========================================================================");
    Console.WriteLine("    Task 10.4 — Part B: I/O-Bound Email Re-send (Parallel.ForEachAsync)   ");
    Console.WriteLine("==========================================================================");

    // 1. Create 30 test orders and customers for order IDs 1001-1030
    var testItems = new List<(Order Order, Customer Customer)>(30);
    for (int id = 1001; id <= 1030; id++)
    {
      var order = new Order
      {
        Id = id,
        CustomerId = id,
        TotalAmount = 50.00m
      };
      var customer = new Customer
      {
        Id = id,
        Name = $"Test Customer #{id}",
        Email = $"customer{id}@test.com"
      };
      testItems.Add((order, customer));
    }

    // Clean previous test emails (1001-1030) if any to ensure clean idempotent run
    var existingEmails = await _jsonFileStore.ReadAsync<List<EmailMessage>>(DataFiles.Emails, cancellationToken);
    if (existingEmails != null && existingEmails.Count > 0)
    {
      var cleanedList = new List<EmailMessage>();
      foreach (var email in existingEmails)
      {
        if (email.OrderId < 1001 || email.OrderId > 1030)
        {
          cleanedList.Add(email);
        }
      }
      await _jsonFileStore.WriteAsync(DataFiles.Emails, cleanedList, cancellationToken);
    }

    // 2. Setup IEmailService with no injected failures
    IEmailService emailService = new EmailService();

    int currentConcurrent = 0;
    int maxConcurrent = 0;

    var parallelOptions = new ParallelOptions
    {
      MaxDegreeOfParallelism = 4,
      CancellationToken = cancellationToken
    };

    var emailSw = Stopwatch.StartNew();

    try
    {
      await Parallel.ForEachAsync(testItems, parallelOptions, async (item, ct) =>
      {
        int cur = Interlocked.Increment(ref currentConcurrent);

        int initialMax;
        do
        {
          initialMax = maxConcurrent;
          if (cur <= initialMax)
            break;
        } while (Interlocked.CompareExchange(ref maxConcurrent, cur, initialMax) != initialMax);

        try
        {
          await emailService.SendConfirmationAsync(item.Order, item.Customer, ct);
        }
        finally
        {
          Interlocked.Decrement(ref currentConcurrent);
        }
      });
    }
    catch (OperationCanceledException)
    {
      Console.WriteLine("I/O-bound email sending was cancelled.");
      throw;
    }

    emailSw.Stop();

    // 3. Verify persistence in emails.json
    var savedEmails = await _jsonFileStore.ReadAsync<List<EmailMessage>>(DataFiles.Emails, cancellationToken);
    var savedOrderIds = new HashSet<int>();
    int duplicates = 0;

    if (savedEmails != null)
    {
      foreach (var e in savedEmails)
      {
        if (e.OrderId >= 1001 && e.OrderId <= 1030)
        {
          if (!savedOrderIds.Add(e.OrderId))
          {
            duplicates++;
          }
        }
      }
    }

    bool allPersisted = savedOrderIds.Count == 30 && duplicates == 0;

    Console.WriteLine($"Total Emails Sent:          {testItems.Count}");
    Console.WriteLine($"Configured Max Parallelism: 4");
    Console.WriteLine($"Measured Max Concurrency:   {maxConcurrent}");
    Console.WriteLine($"Execution Time:             {emailSw.Elapsed.TotalSeconds:F2} s (Expected ~8 s)");
    Console.WriteLine($"Persistence Verification:   {(allPersisted ? "PASS" : "FAIL")} (Saved: {savedOrderIds.Count}/30, Duplicates: {duplicates})");
  }
    
    private sealed class OrderExecutionResult
    {
        public Order Order { get; }
        public double ElapsedSeconds { get; }

        public OrderExecutionResult(Order order, double elapsedSeconds)
        {
            Order = order;
            ElapsedSeconds = elapsedSeconds;
        }
      }
}

