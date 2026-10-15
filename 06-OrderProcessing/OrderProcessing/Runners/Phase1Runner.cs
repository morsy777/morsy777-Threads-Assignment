using OrderProcessing.Persistence;
using OrderProcessing.SyncValidators;
using System.Diagnostics;
using OrderProcessing.Validators;
using OrderProcessing.Repositories;
using OrderProcessing.Models;
namespace OrderProcessing.Runners;

/// <summary>
/// The flow of RunAsync is that:
/// Reset -> Load Incoming Orders -> Validate them async & sync -> Save them in order.json
/// </summary>

public class Phase1Runner
{
  private readonly JsonFileStore _jsonFileStore;
  private readonly DataSeeder _dataSeeder;
  private readonly SyncOrderValidator _syncOrderValidator;
  private readonly IncomingOrderRepository _incomingOrderRepository;
  private readonly OrderValidator _orderValidator;
  public Phase1Runner()
  {
    _jsonFileStore = new JsonFileStore();
    _dataSeeder = new DataSeeder(_jsonFileStore);
    _syncOrderValidator = new SyncOrderValidator();
    _incomingOrderRepository = new IncomingOrderRepository();
    _orderValidator = new OrderValidator();
  }

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    await _dataSeeder.SeedAsync(cancellationToken);

    var orders = await _incomingOrderRepository.GetAllAsync(cancellationToken);

    MeasureSyncExecutionTime(orders);

    await MeasureAsyncExecutionTime(orders, cancellationToken);
  }

  // In this commit, I separate the logic into two methods because
  // I need them in PringSummary(); without the log line after seeding.
  private void MeasureSyncExecutionTime(List<Order> orders)
  {
    var syncStopwatch = Stopwatch.StartNew();

    foreach (var order in orders)
    {
      _syncOrderValidator.ValidateOrder(order);
    }

    syncStopwatch.Stop();
    Console.WriteLine($"Sync run: {syncStopwatch.ElapsedMilliseconds:F2} ms");
  }

  private async Task MeasureAsyncExecutionTime(List<Order> orders, CancellationToken cancellationToken)
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
