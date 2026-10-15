using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Models.Enums;

namespace OrderProcessing.SyncRepositories;

public class SyncOrderRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public SyncOrderRepository()
  {
    _jsonFileStore = new JsonFileStore();
  }

  public void Save(Order order)
  {
    if (order is null)
      return;

    Thread.Sleep(200);
    var orders = _jsonFileStore.Read<List<Order>>(DataFiles.Orders);

    try
    {
      Thread.Sleep(300);
      foreach (var storedOrder in orders)
      {
        if (storedOrder.Id == order.Id)
        {
          UpdateOrder(storedOrder, order);
          _jsonFileStore.Write(DataFiles.Orders, orders);
          return;
        }
      }

      orders.Add(order);
      _jsonFileStore.Write(DataFiles.Orders, orders);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<SyncOrderRepository>("Saving order was cancelled.");
    }
  }

  public List<Order> GetAll()
  {
    Thread.Sleep(200);
    var orders = _jsonFileStore.Read<List<Order>>(DataFiles.Orders);
    return orders;
  }

  public List<Order> GetByStatus(OrderStatus status)
  {
    Thread.Sleep(200);
    var orders = _jsonFileStore.Read<List<Order>>(DataFiles.Orders);

    var filteredOrders = new List<Order>();
    try
    {
      Thread.Sleep(300);
      foreach (var order in orders)
      {
        if (order.Status == status)
          filteredOrders.Add(order);
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<SyncOrderRepository>("Loading orders was cancelled.");
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
