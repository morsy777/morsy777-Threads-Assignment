using OrderProcessing.Models;

namespace OrderProcessing.SyncServices;

public interface ISyncEmailService
{
  void SendConfirmation(Order order, Customer customer);
}
