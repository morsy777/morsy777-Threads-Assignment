using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;
namespace OrderProcessing.Validators;

public class OrderValidator
{
  private readonly CustomerRepository _customerRepository;
  private readonly ProductRepository _productRepository;
  private readonly CouponRepository _couponRepository;
  private readonly OrderRepository _orderRepository;
  public OrderValidator()
  {
    _customerRepository = new CustomerRepository();
    _productRepository = new ProductRepository();
    _couponRepository = new CouponRepository();
    _orderRepository = new OrderRepository();
  }

  public async Task ValidateOrderAsync(Order order, CancellationToken cancellationToken = default)
  {
    var customer = await _customerRepository
      .GetByIdAsync(order.CustomerId, cancellationToken);

    if (customer is null)
    {
      await MarkOrderAsFailedAsync(order, $" Customer #{order.CustomerId} not found", cancellationToken);
      return;
    }

    if (!customer.IsActive)
    {
      await MarkOrderAsFailedAsync(order, "Customer is inactive", cancellationToken);
      return;
    }

    foreach (var item in order.Items)
    {
      var product = await _productRepository
        .GetByIdAsync(item.ProductId, cancellationToken);

      if (product is null)
      {
        await MarkOrderAsFailedAsync(order, $" Product #{item.ProductId} not found", cancellationToken);
        return;
      }

      if (product.StockQuantity < item.Quantity)
      {
        await MarkOrderAsFailedAsync(order, $" Insufficient stock for '{product.Name}'", cancellationToken);
        return;
      }
    }

    // Edge case I expect it, the user may send nothing, whitespace or null.
    // but checking the existance, just validate if the coupon code is right or no.
    if (string.IsNullOrWhiteSpace(order.CouponCode))
    {
      await MarkOrderAsFailedAsync(order, $" Coupon '{order.CouponCode}' should have a value", cancellationToken);
      return;
    }

    var coupon = await _couponRepository
      .GetByCodeAsync(order.CouponCode, cancellationToken);

    if (coupon is null)
    {
      await MarkOrderAsFailedAsync(order, $" Coupon '{order.CouponCode}' does not exist", cancellationToken);
      return;
    }

    if (coupon.ExpiryDate < DateTime.Today)
    {
      await MarkOrderAsFailedAsync(order, $"Coupon '{coupon.Code}' has expired", cancellationToken);
      return;
    }

    await MarkOrderAsPassedAsync(order, cancellationToken);
  }

  private async Task MarkOrderAsPassedAsync(Order order, CancellationToken cancellationToken)
  {
    order.Status = OrderStatus.Validated;
    await _orderRepository.SaveAsync(order, cancellationToken);
  }

  private async Task MarkOrderAsFailedAsync(Order order, string message, CancellationToken cancellationToken)
  {
    order.Status = OrderStatus.Rejected;
    order.FailureReason = message;
    await _orderRepository.SaveAsync(order, cancellationToken);
  }
}
