using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Repositories;

public class CustomerRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public CustomerRepository()
  {
    _jsonFileStore  = new JsonFileStore();
  }

  public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);
    var customers = await _jsonFileStore.ReadAsync<List<Customer>>(DataFiles.Customers, cancellationToken);

    await Task.Delay(300, cancellationToken);
    foreach (var customer in customers)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (customer.Id == id)
        return customer;
    }

    return default;
  }
}
