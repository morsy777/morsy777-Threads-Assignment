using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;
using OrderProcessing.Validators;
namespace OrderProcessing.Services;

public class OrderProcessorV2
{
  private readonly ProductRepository _productRepository;
  private readonly OrderRepository _orderRepository;
  private readonly OrderPricer _orderPricer;
  private readonly OrderValidator _orderValidator;
  private readonly InvoiceGenerator _invoiceGenerator;
  public OrderProcessorV2()
  {
    _productRepository = new ProductRepository();
    _orderRepository = new OrderRepository();
    _orderPricer = new OrderPricer([7]);
    _orderValidator = new OrderValidator();
    _invoiceGenerator = new InvoiceGenerator();
  }

  public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken cancellationToken = default)
  {
    await _orderValidator.ValidateOrderAsync(order, cancellationToken);

    if (order.Status == OrderStatus.Rejected)
      return false;

    var reservedItems = new List<OrderItem>();

    foreach (var item in order.Items)
    {
      var reserved = await _productRepository
        .ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);

      if (reserved)
      {
        reservedItems.Add(item);
        continue;
      }

      // I am sorry the complexity is bad, but I have to do it.
      foreach (var reservedItem in reservedItems)
      {
        await _productRepository
          .ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity, cancellationToken);
      }

      var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

      order.Status = OrderStatus.Rejected;
      order.FailureReason = $"Insufficient stock for '{product?.Name}'";
      await _orderRepository.SaveAsync(order, cancellationToken);

      return false;
    }

    await _orderPricer.PriceOrderAsync(order, cancellationToken);

    // Task 8.5: CPU-bound invoice generation
    await _invoiceGenerator.GenerateAsync(order, cancellationToken);

    order.Status = OrderStatus.Completed;
    await _orderRepository.SaveAsync(order, cancellationToken);

    return true;
  }
}
