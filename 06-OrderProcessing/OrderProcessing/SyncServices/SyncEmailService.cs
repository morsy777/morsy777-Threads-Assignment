using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.SyncServices;

public class SyncEmailService : ISyncEmailService
{
  private static readonly Lock _emailLock = new();
  private readonly Dictionary<int, int> _failAttempts = [];
  private readonly Dictionary<int, int> _attemptCounts = [];
  private readonly JsonFileStore _jsonFileStore;

  public SyncEmailService() : this(null) { }

  public SyncEmailService(IDictionary<int, int>? failAttempts = null)
  {
    _jsonFileStore = new JsonFileStore();
    if (failAttempts != null)
    {
      foreach (var kvp in failAttempts)
      {
        _failAttempts[kvp.Key] = kvp.Value;
      }
    }
  }

  public int GetAttemptCount(int orderId)
  {
    lock (_attemptCounts)
    {
      if (_attemptCounts.TryGetValue(orderId, out int count))
        return count;
      return 0;
    }
  }

  public void SendConfirmation(Order order, Customer customer)
  {
    if (order is null)
    {
      Logger.Error<SyncEmailService>("Order cannot be null.");
      return;
    }

    if (customer is null)
    {
      Logger.Error<SyncEmailService>("Customer cannot be null.");
      return;
    }

    int currentAttempt;
    lock (_attemptCounts)
    {
      if (!_attemptCounts.TryGetValue(order.Id, out currentAttempt))
        currentAttempt = 0;
      currentAttempt++;
      _attemptCounts[order.Id] = currentAttempt;
    }

    Thread.Sleep(200);

    if (_failAttempts.TryGetValue(order.Id, out int maxFailures) && currentAttempt <= maxFailures)
    {
      Logger.Error<SyncEmailService>($"Email attempt #{currentAttempt} for Order #{order.Id} failed (simulated SMTP failure).");
      throw new SmtpUnavailableException($"SMTP server unavailable for Order #{order.Id} (attempt #{currentAttempt})");
    }

    if (string.IsNullOrWhiteSpace(customer.Email))
    {
      customer.GenerateEmail();
    }

    var emailMessage = new EmailMessage
    {
      Id = Guid.NewGuid(),
      OrderId = order.Id,
      To = customer.Email,
      Subject = $"Order Confirmation #{order.Id}",
      Body = $"Dear {customer.Name}, your order #{order.Id} with total amount ${order.TotalAmount:F2} has been confirmed.",
      SentAt = DateTime.Now,
      Attempts = currentAttempt,
      LastError = null
    };

    lock (_emailLock)
    {
      var emails = _jsonFileStore.Read<List<EmailMessage>>(DataFiles.Emails);
      emails ??= [];

      Thread.Sleep(300);

      emails.Add(emailMessage);

      _jsonFileStore.Write(DataFiles.Emails, emails);
      Logger.Success<SyncEmailService>($"Confirmation email sent to {customer.Email} for Order #{order.Id} (attempt #{currentAttempt}).");
    }
  }
}
