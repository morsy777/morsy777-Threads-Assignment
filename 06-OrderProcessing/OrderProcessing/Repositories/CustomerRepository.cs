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
    try
    {
      var customers = await _jsonFileStore.ReadAsync<List<Customer>>(DataFiles.Customers, cancellationToken);

      foreach (var customer in customers)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (customer.Id == id)
          return customer;
      }
    }
    catch(OperationCanceledException)
    {
      Logger.Error<CustomerRepository>("Customer search was cancelled.");
    }
    return default;
  }
}
