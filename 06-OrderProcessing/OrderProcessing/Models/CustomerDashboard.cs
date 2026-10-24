namespace OrderProcessing.Models;

public class CustomerDashboard
{
  public Customer Customer { get; set; } = default!;

  public List<Order> Orders { get; set; } = [];

  public List<PaymentTransaction> Payments { get; set; } = [];

  public List<EmailMessage> Emails { get; set; } = [];
}