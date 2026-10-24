using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.SyncRepositories;

namespace OrderProcessing.SyncServices;

public class SyncOrderPricer
{
  private readonly IEnumerable<int> _failShippingForOrderIds;
  private readonly SyncProductRepository _productRepository;
  private readonly SyncOrderRepository _orderRepository;

  public SyncOrderPricer(IEnumerable<int> failShippingForOrderIds)
  {
    _failShippingForOrderIds = failShippingForOrderIds;
    _productRepository = new SyncProductRepository();
    _orderRepository = new SyncOrderRepository();
  }

  public decimal CalculateSubTotal(Order order)
  {
    var existingOrder = _orderRepository.GetById(order.Id);
    if (existingOrder is null)
      return 0m;

    decimal subtotal = 0m;
    foreach (var item in order.Items)
    {
      var product = _productRepository.GetById(item.ProductId);
      if (product is null)
        continue;

      item.UnitPrice = product.Price;
      subtotal += item.UnitPrice * item.Quantity;
    }

    order.SubTotal = subtotal;
    _orderRepository.Save(order);
    return subtotal;
  }

  public decimal CalculateShippingCost(Order order)
  {
    foreach (var id in _failShippingForOrderIds)
    {
      if (id == order.Id)
        throw new ShippingProviderUnavailableException("Shipping provider unavailable");
    }

    var subtotal = order.SubTotal;
    if (subtotal >= 1000)
    {
      order.ShippingCost = 0m;
      _orderRepository.Save(order);
      return 0m;
    }

    int totalQuantity = 0;
    decimal totalWeight = 0m;

    foreach (var item in order.Items)
    {
      var product = _productRepository.GetById(item.ProductId);
      if (product is not null)
        totalWeight += product.WeightKg * item.Quantity;

      totalQuantity += item.Quantity;
    }

    var shippingCost = 5.00m + (1.50m * totalQuantity) + (2.00m * totalWeight);
    order.ShippingCost = shippingCost;
    _orderRepository.Save(order);
    return shippingCost;
  }
}
