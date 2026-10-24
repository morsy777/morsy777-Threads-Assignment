using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.SyncRepositories;

namespace OrderProcessing.SyncServices;

public class SyncCustomerService
{
  private readonly SyncCustomerRepository _customerRepository;

  public SyncCustomerService()
  {
    _customerRepository = new SyncCustomerRepository();
  }

  public Customer GetCustomer(int id)
  {
    Thread.Sleep(300);
    var customer = _customerRepository.GetById(id);

    if (customer is null)
      throw new CustomerNotFoundException($"Customer #{id} not found");

    return customer;
  }
}
