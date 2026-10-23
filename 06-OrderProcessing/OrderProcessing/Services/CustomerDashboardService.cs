using System.Diagnostics;
using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class CustomerDashboardService
{
  private readonly CustomerService _customerService;
  private readonly OrderRepository _orderRepository;
  private readonly IPaymentGateway _paymentGateway;
  private readonly IEmailService _emailService;
  private readonly JsonFileStore _jsonFileStore;
  private readonly HashSet<int> _failPaymentsForCustomerIds = [];

  public long CustomerLoadMs { get; private set; }
  public long OrdersLoadMs { get; private set; }
  public long PaymentsLoadMs { get; private set; }
  public long EmailsLoadMs { get; private set; }

  public CustomerDashboardService() : this(null) { }

  public CustomerDashboardService(IEnumerable<int>? failPaymentsForCustomerIds)
  {
    _customerService = new CustomerService();
    _orderRepository = new OrderRepository();
    _paymentGateway = new PaymentGateway();
    _emailService = new EmailService();
    _jsonFileStore = new JsonFileStore();

    if (failPaymentsForCustomerIds != null)
    {
      foreach (var id in failPaymentsForCustomerIds)
      {
        _failPaymentsForCustomerIds.Add(id);
      }
    }
  }

  public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken cancellationToken = default)
  {
    var customerTask = LoadCustomerProfileAsync(customerId, cancellationToken);
    var ordersTask = LoadOrdersAsync(customerId, cancellationToken);
    var paymentsTask = LoadPaymentsAsync(customerId, cancellationToken);
    var emailsTask = LoadEmailsAsync(customerId, cancellationToken);

    var allTasks = Task.WhenAll(customerTask, ordersTask, paymentsTask, emailsTask);

    try
    {
      await allTasks;
    }
    catch
    {
      if (allTasks.Exception != null)
      {
        throw allTasks.Exception;
      }
      throw;
    }

    return new CustomerDashboard
    {
      Customer = customerTask.Result,
      Orders = ordersTask.Result,
      Payments = paymentsTask.Result,
      Emails = emailsTask.Result
    };
  }

  public async Task<CustomerDashboard> LoadSequentialAsync(int customerId, CancellationToken cancellationToken = default)
  {
    var customer = await LoadCustomerProfileAsync(customerId, cancellationToken);
    var orders = await LoadOrdersAsync(customerId, cancellationToken);
    var payments = await LoadPaymentsAsync(customerId, cancellationToken);
    var emails = await LoadEmailsAsync(customerId, cancellationToken);

    return new CustomerDashboard
    {
      Customer = customer,
      Orders = orders,
      Payments = payments,
      Emails = emails
    };
  }

  private async Task<Customer> LoadCustomerProfileAsync(int customerId, CancellationToken cancellationToken)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      return await _customerService.GetCustomerAsync(customerId, cancellationToken);
    }
    finally
    {
      sw.Stop();
      CustomerLoadMs = sw.ElapsedMilliseconds;
    }
  }

  private async Task<List<Order>> LoadOrdersAsync(int customerId, CancellationToken cancellationToken)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      return await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
    }
    finally
    {
      sw.Stop();
      OrdersLoadMs = sw.ElapsedMilliseconds;
    }
  }

  private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId, CancellationToken cancellationToken)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      if (_failPaymentsForCustomerIds.Contains(customerId))
      {
        await Task.Delay(500, cancellationToken);
        throw new TimeoutException("Payments store timeout");
      }

      await Task.Delay(200, cancellationToken);
      var payments = await _jsonFileStore.ReadAsync<List<PaymentTransaction>>(DataFiles.Payments, cancellationToken);
      var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
      if (orders == null || orders.Count == 0)
      {
        orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.IncomingOrders, cancellationToken);
      }
      await Task.Delay(300, cancellationToken);

      var orderIds = new HashSet<int>();
      if (orders != null)
      {
        foreach (var order in orders)
        {
          if (order.CustomerId == customerId)
          {
            orderIds.Add(order.Id);
          }
        }
      }

      var customerPayments = new List<PaymentTransaction>();
      if (payments != null)
      {
        foreach (var payment in payments)
        {
          cancellationToken.ThrowIfCancellationRequested();
          if (orderIds.Contains(payment.OrderId))
          {
            customerPayments.Add(payment);
          }
        }
      }

      return customerPayments;
    }
    finally
    {
      sw.Stop();
      PaymentsLoadMs = sw.ElapsedMilliseconds;
    }
  }

  private async Task<List<EmailMessage>> LoadEmailsAsync(int customerId, CancellationToken cancellationToken)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await Task.Delay(200, cancellationToken);
      var emails = await _jsonFileStore.ReadAsync<List<EmailMessage>>(DataFiles.Emails, cancellationToken);
      var orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.Orders, cancellationToken);
      if (orders == null || orders.Count == 0)
      {
        orders = await _jsonFileStore.ReadAsync<List<Order>>(DataFiles.IncomingOrders, cancellationToken);
      }
      await Task.Delay(300, cancellationToken);

      var orderIds = new HashSet<int>();
      if (orders != null)
      {
        foreach (var order in orders)
        {
          if (order.CustomerId == customerId)
          {
            orderIds.Add(order.Id);
          }
        }
      }

      var customerEmails = new List<EmailMessage>();
      if (emails != null)
      {
        foreach (var email in emails)
        {
          cancellationToken.ThrowIfCancellationRequested();
          if (orderIds.Contains(email.OrderId))
          {
            customerEmails.Add(email);
          }
        }
      }

      return customerEmails;
    }
    finally
    {
      sw.Stop();
      EmailsLoadMs = sw.ElapsedMilliseconds;
    }
  }
}