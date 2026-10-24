using OrderProcessing.Persistence;
using OrderProcessing.SyncValidators;
using System.Diagnostics;
using OrderProcessing.Validators;
using OrderProcessing.Repositories;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence.Constants;
namespace OrderProcessing.Runners;

/// <summary>
/// The flow of RunAsync is that:
/// Reset -> Load Incoming Orders -> Validate them async & sync -> Save them in order.json with the new status (validated/rejected)
/// </summary>

public class Phase1Runner
{
  private readonly JsonFileStore _jsonFileStore;
  private readonly DataSeeder _dataSeeder;
  private readonly OrderRepository _orderRepository;
  private readonly IncomingOrderRepository _incomingOrderRepository;
  private readonly SyncOrderValidator _syncOrderValidator;
  private readonly OrderValidator _orderValidator;
  public Phase1Runner()
  {
    _jsonFileStore = new JsonFileStore();
    _dataSeeder = new DataSeeder(_jsonFileStore);
    _orderRepository = new OrderRepository();
    _incomingOrderRepository = new IncomingOrderRepository();
    _syncOrderValidator = new SyncOrderValidator();
    _orderValidator = new OrderValidator();
  }

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    await _dataSeeder.SeedAsync(cancellationToken);

    var orders = await _incomingOrderRepository.GetAllAsync(cancellationToken);

    MeasureSyncOrderValidationTime(orders);

    await MeasureAsyncOrderValidationTime(orders, cancellationToken);
  }

  public async Task LogSummary(CancellationToken cancellationToken = default)
  {
    var orders = await _orderRepository.GetAllAsync(cancellationToken);

    var validOrders = await _orderRepository.GetByStatusAsync(OrderStatus.Validated, cancellationToken);
    var rejectedOrders = await _orderRepository.GetByStatusAsync(OrderStatus.Rejected, cancellationToken);

    Console.WriteLine("\n===== Validation Summary =====");
    Console.WriteLine($"Total Orders: {orders.Count}");
    Console.WriteLine($"Validated: {validOrders.Count}");
    Console.WriteLine($"Rejected: {rejectedOrders.Count}");

    LogRejectedOrders(rejectedOrders);

    MeasureSyncOrderValidationTime(orders);
    await MeasureAsyncOrderValidationTime(orders, cancellationToken);

    Console.WriteLine($"{DataFiles.Orders} now contains {orders.Count} records.");
  }

  private static void LogRejectedOrders(List<Order> rejectedOrders)
  {
    foreach (var order in rejectedOrders)
      Console.WriteLine($"- Order #{order.Id}: {order.FailureReason}");
  }

    // I separate the logic into two methods because I need them
    // in PringSummary(); without the log line after seeding.
  private void MeasureSyncOrderValidationTime(List<Order> orders)
  {
    var syncStopwatch = Stopwatch.StartNew();

    foreach (var order in orders)
    {
      _syncOrderValidator.ValidateOrder(order);
    }

    syncStopwatch.Stop();
    Console.WriteLine($"Sync run: {syncStopwatch.ElapsedMilliseconds:F2} ms");
  }

  private async Task MeasureAsyncOrderValidationTime(List<Order> orders, CancellationToken cancellationToken)
  {
    var asyncStopwatch = Stopwatch.StartNew();

    foreach (var order in orders)
    {
      await _orderValidator.ValidateOrderAsync(order, cancellationToken);
    }

    asyncStopwatch.Stop();
    Console.WriteLine($"Async run: {asyncStopwatch.ElapsedMilliseconds:F2} ms");
  }
}
