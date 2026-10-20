using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class InventoryService
{
  private readonly ProductRepository _productRepository;
  private readonly OrderRepository _orderRepository;

  public InventoryService()
  {
    _productRepository = new ProductRepository();
    _orderRepository = new OrderRepository();
  }

  public async Task<bool> ReserveStockAsync(Order order, CancellationToken cancellationToken = default)
  {
    if (order is null)
      return false;

    var reservedItems = new List<OrderItem>();

    try
    {
      foreach (var item in order.Items)
      {
        var reserved = await _productRepository
          .ReserveStockAsync(item.ProductId, item.Quantity, cancellationToken);

        if (reserved)
        {
          reservedItems.Add(item);
          continue;
        }

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

      return true;
    }
    catch
    {
      foreach (var reservedItem in reservedItems)
      {
        await _productRepository
          .ReleaseStockAsync(reservedItem.ProductId, reservedItem.Quantity, CancellationToken.None);
      }

      // To preseve the stack trace to make debugging easier
      throw;
    }
  }

  public async Task ReleaseStockAsync(Order order, CancellationToken cancellationToken = default)
  {
    if (order is null)
      return;

    foreach (var item in order.Items)
    {
      await _productRepository
        .ReleaseStockAsync(item.ProductId, item.Quantity, cancellationToken);
    }
  }
}
