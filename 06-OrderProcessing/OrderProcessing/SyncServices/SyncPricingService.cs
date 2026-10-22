using OrderProcessing.Exceptions;
using OrderProcessing.Models;
using OrderProcessing.Services;
using OrderProcessing.SyncRepositories;

namespace OrderProcessing.SyncServices;

public class SyncPricingService
{
  private readonly SyncOrderPricer _orderPricer;
  private readonly SyncCustomerRepository _customerRepository;
  private readonly SyncCouponRepository _couponRepository;
  private readonly SyncOrderRepository _orderRepository;

  public SyncPricingService() : this(new SyncOrderPricer([])) { }

  public SyncPricingService(SyncOrderPricer? orderPricer)
  {
    _orderPricer = orderPricer ?? new SyncOrderPricer([]);
    _customerRepository = new SyncCustomerRepository();
    _couponRepository = new SyncCouponRepository();
    _orderRepository = new SyncOrderRepository();
  }

  public decimal CalculateTotal(Order order)
  {
    if (order is null)
      return 0m;

    Thread.Sleep(200);

    _orderPricer.CalculateSubTotal(order);

    order.Discount = CalculateDiscount(order);
    order.ShippingCost = _orderPricer.CalculateShippingCost(order);
    order.TotalAmount = order.SubTotal - order.Discount + order.ShippingCost;

    Thread.Sleep(300);
    _orderRepository.Save(order);

    return order.TotalAmount;
  }

  private decimal CalculateDiscount(Order order)
  {
    var customer = _customerRepository.GetById(order.CustomerId);
    if (customer is null)
      return 0m;

    decimal loyaltyDiscount = OrderPricer.CalculateLoyaltyDiscount(customer);
    decimal couponPercent = 0m;

    if (!string.IsNullOrWhiteSpace(order.CouponCode))
    {
      var coupon = _couponRepository.GetByCode(order.CouponCode);

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
