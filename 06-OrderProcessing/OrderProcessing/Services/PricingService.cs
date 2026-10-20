using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class PricingService
{
  private readonly OrderPricer _orderPricer;
  private readonly ProductRepository _productRepository;
  private readonly CustomerRepository _customerRepository;
  private readonly CouponRepository _couponRepository;
  private readonly OrderRepository _orderRepository;

  public PricingService()
  {
    _orderPricer = new OrderPricer(new int[] { 7 });
    _productRepository = new ProductRepository();
    _customerRepository = new CustomerRepository();
    _couponRepository = new CouponRepository();
    _orderRepository = new OrderRepository();
  }

  public async Task<decimal> CalculateTotalAsync(Order order, CancellationToken cancellationToken = default)
  {
    if (order is null)
      return 0m;

    await Task.Delay(200, cancellationToken);

    await _orderPricer.CalculateSubTotalAsync(order, cancellationToken);

    var discountTask = CalculateDiscountAsync(order, cancellationToken);
    var shippingTask = _orderPricer.CalculateShippingCostAsync(order, cancellationToken);

    await Task.WhenAll(discountTask, shippingTask);

    order.Discount = await discountTask;
    order.ShippingCost = await shippingTask;
    order.TotalAmount = order.SubTotal - order.Discount + order.ShippingCost;

    await Task.Delay(300, cancellationToken);
    await _orderRepository.SaveAsync(order, cancellationToken);

    return order.TotalAmount;
  }

  private async Task<decimal> CalculateDiscountAsync(Order order, CancellationToken cancellationToken)
  {
    var customer = await _customerRepository.GetByIdAsync(order.CustomerId, cancellationToken);

    if (customer is null)
      return 0m;

    decimal loyaltyDiscount = OrderPricer.CalculateLoyaltyDiscount(customer);

    decimal couponPercent = 0m;

    if (!string.IsNullOrWhiteSpace(order.CouponCode))
    {
      var coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, cancellationToken);

      if (coupon is null)
        throw new InvalidCouponException($"Coupon '{order.CouponCode}' not found");

      if (coupon.ExpiryDate < DateTime.Today)
        throw new InvalidCouponException($"Coupon '{coupon.Code}' has expired");

      couponPercent = coupon.DiscountPercent;
    }
    else if (order.CouponCode is not null)
    {
      throw new InvalidCouponException("Coupon is invalid");
    }

    decimal discountPercentage = loyaltyDiscount + couponPercent;
    if (discountPercentage > 30m)
      discountPercentage = 30m;

    decimal discount = order.SubTotal * discountPercentage / 100m;
    order.Discount = discount;

    return discount;
  }
}
