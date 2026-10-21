using OrderProcessing.Models;

namespace OrderProcessing.Services;

public interface IEmailService
{
  Task SendConfirmationAsync(Order order, Customer customer, CancellationToken cancellationToken = default);
}
