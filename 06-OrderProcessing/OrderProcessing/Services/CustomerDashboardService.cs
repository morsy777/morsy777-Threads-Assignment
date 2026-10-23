using OrderProcessing.Models;
using OrderProcessing.Repositories;
namespace OrderProcessing.Services;

public class CustomerDashboardService
{
  private readonly CustomerService _customerService;
  private readonly OrderRepository _orderRepository;
  private readonly IPaymentGateway _paymentGateway;
  private readonly IEmailService _emailService;
  public CustomerDashboardService() : this(null){ }
  public CustomerDashboardService(IEnumerable<int>? timeoutOrderIds)
  {
      _customerService = new CustomerService();
      _orderRepository = new OrderRepository();
      _paymentGateway = new PaymentGateway(timeoutOrderIds ?? []);
      _emailService = new EmailService();
  }

  public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken cancellationToken = default)
  {
    var customerTask = _customerService.GetCustomerAsync(customerId, cancellationToken);
    _ = customerTask;
    return null!;
  }
}