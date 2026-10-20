using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class CustomerService
{
  private readonly CustomerRepository _customerRepository;

  public CustomerService()
  {
    _customerRepository = new CustomerRepository();
  }

  public async Task<Customer> GetCustomerAsync(int id, CancellationToken cancellationToken = default)
  {
    var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);

    if (customer is null)
      throw new CustomerNotFoundException($"Customer #{id} not found");

    return customer;
  }
}
