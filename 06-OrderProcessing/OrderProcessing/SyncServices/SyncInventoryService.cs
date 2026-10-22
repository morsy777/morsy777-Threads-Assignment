using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.SyncRepositories;

namespace OrderProcessing.SyncServices;

public class SyncInventoryService
{
  private readonly SyncProductRepository _productRepository;
  private readonly SyncOrderRepository _orderRepository;

  public SyncInventoryService()
  {
    _productRepository = new SyncProductRepository();
    _orderRepository = new SyncOrderRepository();
  }

  public bool ReserveStock(Order order)
  {
    if (order is null)
      return false;

    var reservedItems = new List<OrderItem>();

    try
    {
      foreach (var item in order.Items)
      {
        var reserved = _productRepository.ReserveStock(item.ProductId, item.Quantity);

        if (reserved)
        {
          reservedItems.Add(item);
          continue;
        }

        foreach (var reservedItem in reservedItems)
        {
          _productRepository.ReleaseStock(reservedItem.ProductId, reservedItem.Quantity);
        }

        var product = _productRepository.GetById(item.ProductId);

        order.Status = OrderStatus.Rejected;
        order.FailureReason = $"Insufficient stock for '{product?.Name}'";
        _orderRepository.Save(order);

        return false;
      }

      return true;
    }
    catch
    {
      foreach (var reservedItem in reservedItems)
      {
        _productRepository.ReleaseStock(reservedItem.ProductId, reservedItem.Quantity);
      }

      throw;
    }
  }

  public void ReleaseStock(Order order)
  {
    if (order is null)
      return;

    foreach (var item in order.Items)
    {
      _productRepository.ReleaseStock(item.ProductId, item.Quantity);
    }
  }
}
