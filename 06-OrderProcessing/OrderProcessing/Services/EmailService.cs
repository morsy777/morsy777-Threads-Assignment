using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Services;

public class EmailService : IEmailService
{
  private static readonly SemaphoreSlim _emailLock = new(1, 1);
  private readonly JsonFileStore _jsonFileStore;

  public EmailService()
  {
    _jsonFileStore = new JsonFileStore();
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
      Attempts = 1,
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
      await Task.Delay(200, cancellationToken);

      var emails = await _jsonFileStore.ReadAsync<List<EmailMessage>>(DataFiles.Emails, cancellationToken);
      emails ??= [];

      await Task.Delay(300, cancellationToken);

      emails.Add(emailMessage);

      await _jsonFileStore.WriteAsync(DataFiles.Emails, emails, cancellationToken);
      Logger.Success<EmailService>($"Confirmation email sent to {customer.Email} for Order #{order.Id}.");
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
