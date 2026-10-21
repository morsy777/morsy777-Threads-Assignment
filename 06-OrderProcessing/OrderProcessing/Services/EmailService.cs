using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Services;

public class EmailService : IEmailService
{
  private static readonly SemaphoreSlim _emailLock = new(1, 1);
  private readonly Dictionary<int, int> _failAttempts = [];
  private readonly Dictionary<int, int> _attemptCounts = [];
  private readonly JsonFileStore _jsonFileStore;

  public EmailService() : this(null) { }

  public EmailService(IDictionary<int, int>? failAttempts = null)
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



  public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken cancellationToken = default)
  {
    if (order is null)
    {
      Logger.Error<EmailService>("Order cannot be null.");
      return;
    }

    if (customer is null)
    {
      Logger.Error<EmailService>("Customer cannot be null.");
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

    await Task.Delay(200, cancellationToken);

    if (_failAttempts.TryGetValue(order.Id, out int maxFailures) && currentAttempt <= maxFailures)
    {
      Logger.Error<EmailService>($"Email attempt #{currentAttempt} for Order #{order.Id} failed (simulated SMTP failure).");
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

    try
    {
      await _emailLock.WaitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
      Logger.Error<EmailService>($"Sending confirmation email for Order #{order.Id} was cancelled.");
      return;
    }

    try
    {
      var emails = await _jsonFileStore.ReadAsync<List<EmailMessage>>(DataFiles.Emails, cancellationToken);
      emails ??= [];

      await Task.Delay(300, cancellationToken);

      emails.Add(emailMessage);

      await _jsonFileStore.WriteAsync(DataFiles.Emails, emails, cancellationToken);
      Logger.Success<EmailService>($"Confirmation email sent to {customer.Email} for Order #{order.Id} (attempt #{currentAttempt}).");
    }
    catch (OperationCanceledException)
    {
      Logger.Error<EmailService>($"Sending confirmation email for Order #{order.Id} was cancelled.");
      throw;
    }
    finally
    {
      _emailLock.Release();
    }
  }
}
