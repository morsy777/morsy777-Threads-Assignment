using OrderProcessing.Repositories;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.SyncRepositories;

namespace OrderProcessing.SyncValidators;

public class SyncOrderValidator
{
  private readonly SyncCustomerRepository _syncCustomerRepository;
  private readonly SyncProductRepository _syncProductRepository;
  private readonly SyncCouponRepository _syncCouponRepository;
  private readonly SyncOrderRepository _syncOrderRepository;
  public SyncOrderValidator()
  {
    _syncCustomerRepository = new SyncCustomerRepository();
    _syncProductRepository = new SyncProductRepository();
    _syncCouponRepository = new SyncCouponRepository();
    _syncOrderRepository = new SyncOrderRepository();
  }

  public void ValidateOrder(Order order)
  {
    var customer = _syncCustomerRepository
      .GetById(order.CustomerId);

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
      var product = _syncProductRepository
        .GetById(item.ProductId);

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

    if (string.IsNullOrWhiteSpace(order.CouponCode))
    {
      MarkOrderAsFailed(order, $" Coupon '{order.CouponCode}' should have a value");
      return;
    }

    var coupon = _syncCouponRepository
      .GetByCode(order.CouponCode);

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

    _syncOrderRepository.Save(order);
  }

  private static void MarkOrderAsPassed(Order order)
    => order.Status = OrderStatus.Validated;

  private static void MarkOrderAsFailed(Order order, string message)
  {
    order.Status = OrderStatus.Rejected;
    order.FailureReason = message;
  }
}
