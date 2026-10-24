using OrderProcessing.Models;

namespace OrderProcessing.Services;

public interface IShippingCarrier
{
    string Name { get; }
    Task<decimal> GetQuoteAsync(Order order, CancellationToken ct);
}
