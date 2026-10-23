using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Models.Enums;

namespace OrderProcessing.Repositories;

public class OrderRepository
{
  private static readonly SemaphoreSlim _saveLock = new(1, 1);
  private readonly HashSet<int> _failSaveForOrderIds = new();
  private readonly JsonFileStore _jsonFileStore;

  public OrderRepository() : this(null) { }

  public OrderRepository(IEnumerable<int>? failSaveForOrderIds = null)
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


  public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
  {
    if (order is null)
      return;

    try
    {
      await _saveLock.WaitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<OrderRepository>("Saving order was cancelled.");
      return;
    }

    try
    {
      await Task.Delay(200, cancellationToken);
      var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
      orders ??= [];

      await Task.Delay(300, cancellationToken);

      if (order.Status == OrderStatus.Completed && _failSaveForOrderIds.Contains(order.Id))
      {
        Logger.Error<OrderRepository>($"Database save failed for order #{order.Id} (simulated connection loss).");
        throw new DatabaseWriteException("Connection lost during commit");
      }

      bool updated = false;

      foreach (var storedOrder in orders)
      {
        cancellationToken.ThrowIfCancellationRequested();

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

      await _jsonFileStore.WriteAsync(DataFiles.Orders, orders, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<OrderRepository>("Saving order was cancelled.");
    }
    finally
    {
      _saveLock.Release();
    }
  }

  public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);
    var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
    return orders;
  }

  public async Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);
    var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);

    var filteredOrders = new List<Order>();
    try
    {
      await Task.Delay(300, cancellationToken);

      foreach (var order in orders)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (order.Status == status)
          filteredOrders.Add(order);
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<OrderRepository>("Loading orders was cancelled.");
    }

    return filteredOrders;
  }

  public async Task<List<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);
    var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
    if (orders == null || orders.Count == 0)
    {
      orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.IncomingOrders, cancellationToken);
    }

    var filteredOrders = new List<Order>();
    try
    {
      await Task.Delay(300, cancellationToken);

      if (orders != null)
      {
        foreach (var order in orders)
        {
          cancellationToken.ThrowIfCancellationRequested();

          if (order.CustomerId == customerId)
            filteredOrders.Add(order);
        }
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<OrderRepository>($"Loading orders for customer #{customerId} was cancelled.");
    }

    return filteredOrders;
  }

  // Its not required in the assingment by I added it to achinve DRY and validate oreder existance.
  public async Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
  {
    if (orderId <= 0)
    {
      Logger.Error<OrderRepository>("Invalid Order Id");
      return default;
    }

    var orders = await GetAllAsync(cancellationToken);

    try
    {
      await Task.Delay(300, cancellationToken);

      foreach (var order in orders)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (order.Id == orderId)
          return order;
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<OrderRepository>($"Getting order #{orderId} was cancelled.");
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
