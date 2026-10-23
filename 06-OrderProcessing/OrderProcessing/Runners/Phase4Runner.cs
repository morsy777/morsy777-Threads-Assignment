using System.Diagnostics;
using System.Globalization;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence;
using OrderProcessing.Repositories;
using OrderProcessing.Services;

namespace OrderProcessing.Runners;

public class Phase4Runner
{
    private readonly JsonFileStore _jsonFileStore;
    private readonly DataSeeder _dataSeeder;
    private readonly IncomingOrderRepository _incomingOrderRepository;
    private OrderProcessor _orderProcessor;

    public Phase4Runner()
    {
        _jsonFileStore = new JsonFileStore();
        _dataSeeder = new DataSeeder(_jsonFileStore);
        _incomingOrderRepository = new IncomingOrderRepository();
        _orderProcessor = CreateConfiguredOrderProcessor();
    }
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
      return RunLiveCheckoutAsync(cancellationToken);
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
