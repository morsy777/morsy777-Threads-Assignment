using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Models.Enums;

namespace OrderProcessing.Repositories;

public class OrderRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public OrderRepository()
  {
    _jsonFileStore = new JsonFileStore();
  }

  public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
  {
    if (order is null)
      return;

    var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);

    try
    {
      foreach (var storedOrder in orders)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (storedOrder.Id == order.Id)
        {
          UpdateOrder(storedOrder, order);
          await _jsonFileStore.WriteAsync(DataFiles.Orders, orders, cancellationToken);
          return;
        }
      }

      orders.Add(order);
      await _jsonFileStore.WriteAsync(DataFiles.Orders, orders, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<OrderRepository>("Saving order was cancelled.");
    }
  }

  public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
    return orders;
  }

  public async Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
  {
    var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
    var filteredOrders = new List<Order>();
    foreach (var order in orders)
    {
      if (order.Status == status)
        filteredOrders.Add(order);
    }
    return filteredOrders;
  }


  private static void UpdateOrder(Order storedOrder, Order order)
  {
    storedOrder.CustomerId = order.CustomerId;
    storedOrder.Items = order.Items;
    storedOrder.CouponCode = order.CouponCode;
    storedOrder.CreatedAt = order.CreatedAt;
    storedOrder.Discount = order.Discount;
    storedOrder.FailureReason = order.FailureReason;
    storedOrder.TransactionId = order.TransactionId;
    storedOrder.ShippingCost = order.ShippingCost;
    storedOrder.Status = order.Status;
    storedOrder.SubTotal = order.SubTotal;
    storedOrder.TotalAmount = order.TotalAmount;
  }
}
