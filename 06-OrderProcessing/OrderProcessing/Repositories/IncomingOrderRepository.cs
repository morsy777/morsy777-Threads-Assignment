using OrderProcessing.Persistence;
using OrderProcessing.Models;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Repositories;

public class IncomingOrderRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public IncomingOrderRepository()
  {
    _jsonFileStore = new JsonFileStore();
  }

  public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);
    var incomingOrders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.IncomingOrders, cancellationToken);
    return incomingOrders;
  }
}
