using OrderProcessing.Persistence;
using OrderProcessing.Models;
using OrderProcessing.Exceptions;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.SyncValidators;
using System.Diagnostics;
using OrderProcessing.Validators;
using OrderProcessing.Repositories;
namespace OrderProcessing.Runners;

/// <summary>
/// The flow of this class is that:
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
    // reset then seed 
    await _dataSeeder.SeedAsync(cancellationToken);

    var orders = await _incomingOrderRepository.GetAllAsync(cancellationToken);

    var syncStopwatch = Stopwatch.StartNew();

    foreach (var order in orders)
    {
      _syncOrderValidator.ValidateOrder(order);
    }

    syncStopwatch.Stop();
    Console.WriteLine($"Sync run: {syncStopwatch.ElapsedMilliseconds:F2} ms");


    var asyncStopwatch = Stopwatch.StartNew();

    foreach (var order in orders)
    {
      await _orderValidator.ValidateOrderAsync(order, cancellationToken);
    }

    asyncStopwatch.Stop();
    Console.WriteLine($"Async run: {asyncStopwatch.ElapsedMilliseconds:F2} ms");
  }
}
