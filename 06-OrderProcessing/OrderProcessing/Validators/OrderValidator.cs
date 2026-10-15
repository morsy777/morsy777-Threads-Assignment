using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;
namespace OrderProcessing.Validators;

public class OrderValidator
{
  private readonly CustomerRepository _customerRepository;
  private readonly ProductRepository _productRepository;
  private readonly CouponRepository _couponRepository;
  public OrderValidator()
  {
    _customerRepository = new CustomerRepository();
    _productRepository = new ProductRepository();
    _couponRepository = new CouponRepository();
  }

  public async Task ValidateOrderAsync(Order order, CancellationToken cancellationToken = default)
  {
    var customer = await _customerRepository
      .GetByIdAsync(order.CustomerId, cancellationToken);

    if (customer is null)
    {
      MarkOrderAsFailed(order, $" Customer #{order.CustomerId} not found");
      return;
    }

    if (!customer.IsActive)
    {
      MarkOrderAsFailed(order, "Customer is inactive");
      return;
    }

    foreach (var item in order.Items)
    {
      var product = await _productRepository
        .GetByIdAsync(item.ProductId, cancellationToken);

      if (product is null)
      {
        MarkOrderAsFailed(order, $" Product #{item.ProductId} not found");
        return;
      }

      if (product.StockQuantity < item.Quantity)
      {
        MarkOrderAsFailed(order, $" Insufficient stock for '{product.Name}'");
        return;
      }
    }

    // Edge case I expect it, the user may send nothing, whitespace or null.
    // but checking the existance, just validate if the coupon code is right or no.
    if (string.IsNullOrWhiteSpace(order.CouponCode))
    {
      MarkOrderAsFailed(order, $" Coupon '{order.CouponCode}' should have a value");
      return;
    }

    var coupon = await _couponRepository
      .GetByCodeAsync(order.CouponCode, cancellationToken);

    if (coupon is null)
    {
      MarkOrderAsFailed(order, $" Coupon '{order.CouponCode}' does not exist");
      return;
    }

    if (coupon.ExpiryDate < DateTime.Today)
    {
      MarkOrderAsFailed(order, $"Coupon '{coupon.Code}' has expired");
      return;
    }

    MarkOrderAsPassed(order);
  }

  private static void MarkOrderAsPassed(Order order)
    => order.Status = OrderStatus.Validated;

  private static void MarkOrderAsFailed(Order order, string message)
  {
    order.Status = OrderStatus.Rejected;
    order.FailureReason = message;
  }
}
