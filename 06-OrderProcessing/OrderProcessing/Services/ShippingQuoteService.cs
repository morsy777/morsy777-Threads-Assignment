using System.Diagnostics;
using OrderProcessing.Exceptions;
using OrderProcessing.Models;

namespace OrderProcessing.Services;

public class ShippingQuoteService
{
    private readonly List<IShippingCarrier> _carriers;

    public ShippingQuoteService() : this(null, null, null)
    {
    }

    public ShippingQuoteService(IEnumerable<int>? failForOrderIds) : this(failForOrderIds, failForOrderIds, failForOrderIds)
    {
    }

    public ShippingQuoteService(IEnumerable<int>? fastShipFails, IEnumerable<int>? cairoExpressFails, IEnumerable<int>? nileCourierFails)
    {
        _carriers = new List<IShippingCarrier>(3)
        {
            new FastShip(fastShipFails),
            new CairoExpress(cairoExpressFails),
            new NileCourier(nileCourierFails)
        };
    }

    public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
    {
        ArgumentNullException.ThrowIfNull(carriers);
        _carriers = new List<IShippingCarrier>();
        foreach (var carrier in carriers)
        {
            _carriers.Add(carrier);
        }
    }

    public async Task<ShippingQuote> GetFastestQuoteAsync(Order order, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ct.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        using var internalCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, internalCts.Token);

        var carrierTasks = new List<(IShippingCarrier Carrier, Task<decimal> Task)>(_carriers.Count);
        var pendingCarrierTasks = new List<Task<decimal>>(_carriers.Count);

        foreach (var carrier in _carriers)
        {
            var task = carrier.GetQuoteAsync(order, linkedCts.Token);
            carrierTasks.Add((carrier, task));
            pendingCarrierTasks.Add(task);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token);
        var timeoutTask = Task.Delay(1200, timeoutCts.Token);

        IShippingCarrier? winningCarrier = null;
        decimal winningPrice = 0m;

        while (pendingCarrierTasks.Count > 0)
        {
            var waitList = new List<Task>(pendingCarrierTasks.Count + 1);
            foreach (var t in pendingCarrierTasks)
            {
                waitList.Add(t);
            }
            waitList.Add(timeoutTask);

            Task completedTask;
            try
            {
                completedTask = await Task.WhenAny(waitList);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                internalCts.Cancel();
                await CleanupRemainingCarriersAsync(carrierTasks);
                ct.ThrowIfCancellationRequested();
                throw;
            }

            if (ct.IsCancellationRequested)
            {
                internalCts.Cancel();
                await CleanupRemainingCarriersAsync(carrierTasks);
                ct.ThrowIfCancellationRequested();
            }

            if (completedTask == timeoutTask)
            {
                // Strict 1.2s timeout exceeded without caller cancellation
                break;
            }

            // Remove completed task from pending list
            int completedIndex = -1;
            for (int i = 0; i < pendingCarrierTasks.Count; i++)
            {
                if (pendingCarrierTasks[i] == completedTask)
                {
                    completedIndex = i;
                    break;
                }
            }
            if (completedIndex >= 0)
            {
                pendingCarrierTasks.RemoveAt(completedIndex);
            }

            if (completedTask.IsCompletedSuccessfully)
            {
                var successfulTask = (Task<decimal>)completedTask;
                winningPrice = await successfulTask;

                for (int i = 0; i < carrierTasks.Count; i++)
                {
                    if (carrierTasks[i].Task == completedTask)
                    {
                        winningCarrier = carrierTasks[i].Carrier;
                        break;
                    }
                }

                break;
            }
            else
            {
                // Observe failure exception
                _ = completedTask.Exception;

                // If all carriers failed before timeout, throw immediately
                if (pendingCarrierTasks.Count == 0)
                {
                    internalCts.Cancel();
                    await CleanupRemainingCarriersAsync(carrierTasks);
                    throw new ShippingProviderUnavailableException("All carriers failed");
                }
            }
        }

        // Cancel all still-running carriers
        internalCts.Cancel();

        // Await all carriers to ensure no background tasks are left running and cancellation is logged
        await CleanupRemainingCarriersAsync(carrierTasks);

        stopwatch.Stop();

        var cancelledCarriers = new List<string>();
        foreach (var pair in carrierTasks)
        {
            if (pair.Carrier != winningCarrier && pair.Task.IsCanceled)
            {
                cancelledCarriers.Add(pair.Carrier.Name);
            }
        }

        if (winningCarrier != null)
        {
            return new ShippingQuote
            {
                Carrier = winningCarrier.Name,
                Price = winningPrice,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                CancelledCarriers = cancelledCarriers
            };
        }

        return new ShippingQuote
        {
            Carrier = "Flat rate",
            Price = 25.00m,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            CancelledCarriers = cancelledCarriers
        };
    }

    private static async Task CleanupRemainingCarriersAsync(List<(IShippingCarrier Carrier, Task<decimal> Task)> carrierTasks)
    {
        foreach (var pair in carrierTasks)
        {
            try
            {
                await pair.Task;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is signaled
            }
            catch (Exception)
            {
                // Observed carrier failure
            }
        }
    }
}
