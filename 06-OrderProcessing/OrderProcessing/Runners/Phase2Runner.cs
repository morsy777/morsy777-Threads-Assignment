using OrderProcessing.Persistence;
using OrderProcessing.Repositories;
using OrderProcessing.SyncValidators;
using OrderProcessing.Validators;
using OrderProcessing.Models;
using OrderProcessing.Services;
using OrderProcessing.Models.Enums;
using System.Diagnostics;
using OrderProcessing.Persistence.Constants;
using System.Text.Json;
using OrderProcessing.Exceptions;

namespace OrderProcessing.Runners;

public class Phase2Runner
{
  private readonly JsonFileStore _jsonFileStore;
  private readonly DataSeeder _dataSeeder;
  private readonly OrderRepository _orderRepository;
  private readonly OrderPricer _orderPricer;
  private readonly OrderValidator _orderValidator;
  private readonly OrderProcessorV2 _orderProcessorV2;
  private readonly RaceConditionDemo _raceConditionDemo;
  private readonly IncomingOrderRepository _incomingOrderRepository;
  private readonly ProductRepository _productRepository;
  private readonly ReportGenerator _reportGenerator;

  public Phase2Runner()
  {
    _jsonFileStore = new JsonFileStore();
    _dataSeeder = new DataSeeder(_jsonFileStore);
    _orderRepository = new OrderRepository();
    _orderPricer = new OrderPricer(new int[] {7});
    _orderValidator = new OrderValidator();
    _orderProcessorV2 = new OrderProcessorV2();
    _raceConditionDemo = new RaceConditionDemo();
    _productRepository = new ProductRepository();
    _incomingOrderRepository = new IncomingOrderRepository();
    _reportGenerator = new ReportGenerator();
  }

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    var stopwatch = Stopwatch.StartNew();

    await _dataSeeder.SeedAsync(cancellationToken);
    await _raceConditionDemo.RunAsync(cancellationToken);

    await _dataSeeder.SeedAsync(cancellationToken);

    // await _phase1Runner.RunAsync(cancellationToken);

    await _orderPricer.TestPricingPerformanceAsync(cancellationToken);

    // var orders = await _orderRepository.GetAllAsync(cancellationToken);
    var orders = await _incomingOrderRepository.GetAllAsync(cancellationToken);

    // ValidateOrderAsync() -> validate the order, then save ti with the rihgt status & failureReason.
    // List<Task> tasks = [];
    // foreach (var order in orders)
    // {
    //   var task = _orderValidator.ValidateOrderAsync(order, cancellationToken);
    //   tasks.Add(task);
    // }

    // await Task.WhenAll(tasks);


    var validatedOrdersTasks = new List<(Order Order, Task<bool> Task)>();
    foreach (var order in orders)
    {
      var task = _orderProcessorV2.ProcessValidatedOrderAsync(order, cancellationToken);

      validatedOrdersTasks.Add((order,task));
    }

    if(validatedOrdersTasks.Count > 0)
      Console.WriteLine($"Task[0] Status: {validatedOrdersTasks[0].Task.Status}");

    await Task.Delay(1000, cancellationToken);

    foreach (var pair in validatedOrdersTasks)
    {
      Console.WriteLine($"Task Status: {pair.Task.Status}");
    }


    foreach (var pair in validatedOrdersTasks)
    {
      try
      {
        await pair.Task;
      }
      catch (ShippingProviderUnavailableException ex)
      {
        Console.WriteLine($"Task Status: {pair.Task.Status}");

        pair.Order.Status = OrderStatus.Failed;
        pair.Order.FailureReason = ex.Message;

        foreach (var item in pair.Order.Items)
        {
          await _productRepository
            .ReleaseStockAsync(item.ProductId, item.Quantity, cancellationToken);
        }
      }
      await _orderRepository.SaveAsync(pair.Order, cancellationToken);
    }

    foreach (var pair in validatedOrdersTasks)
    {
      Console.WriteLine($"Task Status (Final): {pair.Task.Status}");
    }

    stopwatch.Stop();

    Console.WriteLine($"Total Execution Time: {stopwatch.ElapsedMilliseconds} ms");

    var finalOrders = await _orderRepository.GetAllAsync(cancellationToken);

    Console.WriteLine($"Orders in orders.json: {finalOrders.Count}");
    Console.WriteLine($"Contains exactly 20 orders: {finalOrders.Count == 20}");

    Task<Report> reportTask = _reportGenerator.GenerateAsync(cancellationToken);
    Console.WriteLine("\nGenerating report...");
    var report = await reportTask;

    PrintReport(report);
  }

  private static void PrintReport(Report report)
  {
    Console.WriteLine("\n=================== Report Summary ===================");
    Console.WriteLine($"Total Orders: {report.TotalOrders}");
    Console.WriteLine($"Completed: {report.CompletedCount}");
    Console.WriteLine($"Rejected: {report.RejectedCount}");
    Console.WriteLine($"Failed: {report.FailedCount}");
    Console.WriteLine($"Payment Failed: {report.PaymentFailedCount}");
    Console.WriteLine($"Total Revenue: ${report.TotalRevenue:F2}");
    Console.WriteLine($"Average Order Value: ${report.AverageOrderValue:F2}");
    Console.WriteLine($"Top Selling Product: {report.TopSellingProduct} ({report.TopSellingQuantity} units)");
    Console.WriteLine("======================================================");
  }
}
