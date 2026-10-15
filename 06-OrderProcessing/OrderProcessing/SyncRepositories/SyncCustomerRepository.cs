using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;

namespace OrderProcessing.SyncRepositories;

public class SyncCustomerRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public SyncCustomerRepository()
  {
    _jsonFileStore  = new JsonFileStore();
  }

  public Customer? GetById(int id)
  {
    try
    {
      Thread.Sleep(200);
      var customers = _jsonFileStore.Read<List<Customer>>(DataFiles.Customers);

      Thread.Sleep(300);
      foreach (var customer in customers)
      {
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
