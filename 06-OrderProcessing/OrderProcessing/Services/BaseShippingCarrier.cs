using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public abstract class BaseShippingCarrier : IShippingCarrier
{
    private readonly HashSet<int> _failOrderIds = [];
    private readonly ProductRepository _productRepository;

    public abstract string Name { get; }
    public abstract decimal PriceFactor { get; }
    public abstract int MinDelayMs { get; }
    public abstract int MaxDelayMs { get; }

    protected BaseShippingCarrier(IEnumerable<int>? failForOrderIds = null, ProductRepository? productRepository = null)
    {
        _productRepository = productRepository ?? new ProductRepository();
        if (failForOrderIds != null)
        {
            foreach (var id in failForOrderIds)
            {
                _failOrderIds.Add(id);
            }
        }
    }

    protected virtual bool ShouldFail(int orderId)
    {
        return _failOrderIds.Contains(orderId);
    }

    public async Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            await Task.Delay(150, ct);

            if (ShouldFail(order.Id))
            {
                throw new ShippingProviderUnavailableException($"{Name} unavailable");
            }

            var delay = Random.Shared.Next(MinDelayMs, MaxDelayMs + 1);
            await Task.Delay(delay, ct);

            var basePrice = await CalculateBaseShippingPriceAsync(order, ct);
            return Math.Round(basePrice * PriceFactor, 2);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{Name}] cancelled");
            throw;
        }
    }

    protected async Task<decimal> CalculateBaseShippingPriceAsync(Order order, CancellationToken ct)
    {
        if (order.SubTotal >= 1000m)
        {
            return 0m;
        }

        int totalQuantity = 0;
        decimal totalWeight = 0m;
        decimal calculatedSubTotal = order.SubTotal;

        foreach (var item in order.Items)
        {
            ct.ThrowIfCancellationRequested();
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            if (product != null)
            {
                totalWeight += product.WeightKg * item.Quantity;
                if (calculatedSubTotal == 0m)
                {
                    calculatedSubTotal += (item.UnitPrice > 0m ? item.UnitPrice : product.Price) * item.Quantity;
                }
            }
            totalQuantity += item.Quantity;
        }

        if (order.SubTotal == 0m && calculatedSubTotal > 0m)
        {
            order.SubTotal = calculatedSubTotal;
        }

        if (order.SubTotal >= 1000m)
        {
            return 0m;
        }

        return 5.00m + (1.50m * totalQuantity) + (2.00m * totalWeight);
    }
}
