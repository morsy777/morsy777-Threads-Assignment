using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.SyncRepositories;

public class SyncOrderRepository
{
  private static readonly Lock _saveLock = new();
  private readonly HashSet<int> _failSaveForOrderIds = [];
  private readonly JsonFileStore _jsonFileStore;

  public SyncOrderRepository() : this(null) { }

  public SyncOrderRepository(IEnumerable<int>? failSaveForOrderIds = null)
  {
    _jsonFileStore = new JsonFileStore();
    if (failSaveForOrderIds != null)
    {
      foreach (var id in failSaveForOrderIds)
      {
        _failSaveForOrderIds.Add(id);
      }
    }
  }

  public void Save(Order order)
  {
    if (order is null)
      return;

    lock (_saveLock)
    {
      Thread.Sleep(200);
      var orders = _jsonFileStore.Read<List<Order>>(DataFiles.Orders);
      orders ??= [];

      try
      {
        Thread.Sleep(300);

        if (order.Status == OrderStatus.Completed && _failSaveForOrderIds.Contains(order.Id))
        {
          Logger.Error<SyncOrderRepository>($"Database save failed for order #{order.Id} (simulated connection loss).");
          throw new DatabaseWriteException("Connection lost during commit");
        }

        bool updated = false;
        foreach (var storedOrder in orders)
        {
          if (storedOrder.Id == order.Id)
          {
            UpdateOrder(storedOrder, order);
            updated = true;
            break;
          }
        }

        if (!updated)
        {
          orders.Add(order);
        }

        _jsonFileStore.Write(DataFiles.Orders, orders);
      }
      catch (OperationCanceledException)
      {
        Logger.Error<SyncOrderRepository>("Saving order was cancelled.");
      }
    }
  }

  public List<Order> GetAll()
  {
    Thread.Sleep(200);
    var orders = _jsonFileStore.Read<List<Order>>(DataFiles.Orders);
    return orders ?? [];
  }

  public List<Order> GetByStatus(OrderStatus status)
  {
    Thread.Sleep(200);
    var orders = _jsonFileStore.Read<List<Order>>(DataFiles.Orders);
    orders ??= [];

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

  public Order? GetById(int orderId)
  {
    if (orderId <= 0)
    {
      Logger.Error<SyncOrderRepository>("Invalid Order Id");
      return default;
    }

    var orders = GetAll();

    try
    {
      Thread.Sleep(300);
      foreach (var order in orders)
      {
        if (order.Id == orderId)
          return order;
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<SyncOrderRepository>($"Getting order #{orderId} was cancelled.");
    }

    return default;
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
