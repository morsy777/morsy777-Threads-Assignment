using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class PricingService
{
  private readonly IEnumerable<int> _failShippingForOrderIds;
  private readonly CustomerRepository _customerRepository;
  private readonly ProductRepository _productRepository;
  private readonly CouponRepository _couponRepository;
  private readonly OrderRepository _orderRepository;
  public PricingService()
  {
    _customerRepository = new CustomerRepository();
    _productRepository = new ProductRepository();
    _couponRepository = new CouponRepository();
    _orderRepository = new OrderRepository();
  }

  public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken cancellationToken = default)
  {
    var isExists = await IsOrderExistAsync(order.Id, cancellationToken);

    if (!isExists)
      return 0m;

    decimal subtotal = 0m;
    foreach (var item in order.Items)
    {
      var product = await _productRepository
        .GetByIdAsync(item.ProductId, cancellationToken);

      if (product is null)
        continue;

      item.UnitPrice = product.Price;

      subtotal += item.UnitPrice * item.Quantity;
    }

    order.SubTotal = subtotal;

    await _orderRepository.SaveAsync(order, cancellationToken);

    return subtotal;
  }

  public async Task<decimal> GetDiscountAsync(Order order, CancellationToken cancellationToken = default)
  {
    var isExists = await IsOrderExistAsync(order.Id, cancellationToken);

    if (!isExists)
      return 0m;

    var customer = await _customerRepository
      .GetByIdAsync(order.CustomerId, cancellationToken);

    if (customer is null)
      return 0m;

    var loyaltyDiscount = CalculateLoyaltyDiscount(customer);
    decimal subtotal = await CalculateSubTotalAsync(order, cancellationToken);

    decimal discountPercent = loyaltyDiscount;

    if (!string.IsNullOrWhiteSpace(order.CouponCode))
    {
      var coupon = await _couponRepository.GetByCodeAsync(order.CouponCode, cancellationToken);

      discountPercent += coupon?.DiscountPercent ?? 0m;
    }

    discountPercent = Math.Min(discountPercent, 30m);

    decimal discount = discountPercent * subtotal / 100;

    order.Discount = discount;

    await _orderRepository.SaveAsync(order, cancellationToken);

    return discount;
  }

  public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken cancellationToken = default)
  { // orderId = 6
    var discount = await GetDiscountAsync(order, cancellationToken); // 9
    var subtotal = order.SubTotal; // 45

    if (subtotal >= 1000)
    {
      order.ShippingCost = 0m;
      order.TotalAmount = subtotal - discount + 0m;

      await _orderRepository.SaveAsync(order, cancellationToken);

      return 0m;
    }

    int totalQuantity = 0; // 1
    decimal totalWeight = 0; // 0.2 * item.Quan(1)

    foreach (var item in order.Items)
    {
      var product = await _productRepository
        .GetByIdAsync(item.ProductId, cancellationToken);

      if (product is not null)
        totalWeight += product.WeightKg * item.Quantity;

      totalQuantity += item.Quantity;
    }

    var shippingCost = 5.00m + (1.50m * totalQuantity) + (2.00m * totalWeight);
    var totalAmount = subtotal - discount + shippingCost;

    order.ShippingCost = shippingCost;
    order.TotalAmount = totalAmount;

    await _orderRepository.SaveAsync(order, cancellationToken);

    return shippingCost;
  }


  // Violates OCP, I will change it.
  // private static decimal CalculateLoyaltyDiscount(Customer customer)
  // {
  //   return customer.LoyaltyTier switch
  //   {
  //     LoyaltyTier.Bronze => 0,
  //     LoyaltyTier.Silver => 5,
  //     LoyaltyTier.Gold => 10,
  //     _ => 0
  //   };
  // }

  private static readonly Dictionary<LoyaltyTier, decimal> LoyaltyDiscounts = new()
  {
    [LoyaltyTier.Bronze] = 0m,
    [LoyaltyTier.Silver] = 5m,
    [LoyaltyTier.Gold] = 10m
  };

  private static decimal CalculateLoyaltyDiscount(Customer customer) =>
    LoyaltyDiscounts.GetValueOrDefault(customer.LoyaltyTier, 0m);

  private async Task<bool> IsOrderExistAsync(int orderId, CancellationToken cancellationToken)
  {
    var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

    return order is not null;
  }

}
