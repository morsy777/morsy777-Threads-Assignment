using System.Diagnostics;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;
using OrderProcessing.Services;
using OrderProcessing.SyncServices;

namespace OrderProcessing.Runners;

public class Phase3Runner
{
    private readonly JsonFileStore _jsonFileStore;
    private readonly DataSeeder _dataSeeder;
    private readonly IncomingOrderRepository _incomingOrderRepository;
    private readonly OrderProcessor _orderProcessor;
    private readonly SyncOrderProcessor _syncOrderProcessor;

    public Phase3Runner()
    {
        _jsonFileStore = new JsonFileStore();
        _dataSeeder = new DataSeeder(_jsonFileStore);
        _incomingOrderRepository = new IncomingOrderRepository();
        _orderProcessor = new OrderProcessor();
        _syncOrderProcessor = new SyncOrderProcessor();
    }

    public async Task RunAsync()
    {
        await RunAsyncOrderProcessingAsync();
        await RunSyncOrderProcessingAsync();
    }

    private async Task RunAsyncOrderProcessingAsync()
    {
        await _dataSeeder.SeedAsync();

        var initialProducts =
            await _jsonFileStore.ReadAsync<List<Product>>(DataFiles.Products);

        var incomingOrders =
            await _incomingOrderRepository.GetAllAsync();

        var processingTasks = new List<Task<bool>>(10);

        var asyncStopwatch = Stopwatch.StartNew();

        foreach (var incomingOrder in incomingOrders.Take(10))
        {
            processingTasks.Add(
                _orderProcessor.ProcessOrderAsync(incomingOrder));
        }

        await Task.WhenAll(processingTasks);

        asyncStopwatch.Stop();

        Console.WriteLine(
            $"Async execution time: " +
            $"{asyncStopwatch.Elapsed.TotalMilliseconds:F2} ms");

        Console.WriteLine();
        Console.WriteLine(
            $"{"Order",-8} {"Status",-14} {"Total",12}  Reason");

        foreach (var incomingOrder in incomingOrders.Take(10))
        {
            Console.WriteLine(
                $"{incomingOrder.Id,-8} " +
                $"{incomingOrder.Status,-14} " +
                $"{incomingOrder.TotalAmount,12:F2}  " +
                $"{incomingOrder.FailureReason ?? "-"}");
        }
    }

    private async Task RunSyncOrderProcessingAsync()
    {
        var syncJsonFileStore = new JsonFileStore();
        var syncDataSeeder = new DataSeeder(syncJsonFileStore);

        await syncDataSeeder.SeedAsync();

        var syncInitialProducts =
            syncJsonFileStore.Read<List<Product>>(DataFiles.Products);

        var syncIncomingOrders =
            await _incomingOrderRepository.GetAllAsync();

        var threads = new List<Thread>(10);

        var syncStopwatch = Stopwatch.StartNew();

        foreach (var incomingOrder in syncIncomingOrders.Take(10))
        {
            var orderToProcess = incomingOrder;

            threads.Add(new Thread(
                () => _syncOrderProcessor.ProcessOrder(orderToProcess)));
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        syncStopwatch.Stop();

        Console.WriteLine(
            $"Sync execution time: " +
            $"{syncStopwatch.Elapsed.TotalMilliseconds:F2} ms");

        Console.WriteLine();
        Console.WriteLine(
            $"{"Order",-8} {"Status",-14} {"Total",12}  Reason");

        foreach (var incomingOrder in syncIncomingOrders.Take(10))
        {
            Console.WriteLine(
                $"{incomingOrder.Id,-8} " +
                $"{incomingOrder.Status,-14} " +
                $"{incomingOrder.TotalAmount,12:F2}  " +
                $"{incomingOrder.FailureReason ?? "-"}");
        }
    }
}