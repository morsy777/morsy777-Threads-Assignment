using System.Text;
using System.Text.Json;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Persistence;

public class DataSeeder(JsonFileStore jsonFileStore)
{
  private readonly JsonFileStore _jsonFileStore = jsonFileStore;

  public async Task SeedAsync(CancellationToken cancellationToken = default)
  {
    string path = Path.Combine(AppContext.BaseDirectory, DataFiles.DataDir);

    if (Directory.Exists(path))
      Directory.Delete(path, recursive: true);

    Directory.CreateDirectory(path);

    await SeedCustomersAsync(cancellationToken);
    await SeedProductsAsync(cancellationToken);
    await SeedCouponsAsync(cancellationToken);
    await SeedIncomingOrdersAsync(cancellationToken);
  }

  public async Task SeedCustomersAsync(CancellationToken cancellationToken = default)
  {
    List<Customer> customers =
    [
      new() { Id = 1, Name = "Ahmed Ali", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
      new() { Id = 2, Name = "Sara Mohamed", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
      new() { Id = 3, Name = "Omar Hassan", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
      new() { Id = 4, Name = "Mona Ibrahim", IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
      new() { Id = 5, Name = "Youssef Adel", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
      new() { Id = 6, Name = "Nour Khaled", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
      new() { Id = 7, Name = "Karim Samir", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
      new() { Id = 8, Name = "Laila Mahmoud", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
      new() { Id = 9, Name = "Hassan Tarek", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
      new() { Id = 10, Name = "Dina Fathy", IsActive = true, LoyaltyTier = LoyaltyTier.Silver }
    ];

    await _jsonFileStore.WriteAsync(DataFiles.Customers, customers, cancellationToken);

    Console.WriteLine($"[Seeder] {DataFiles.Customers} -> 10 records");
  }

  private async Task SeedProductsAsync(CancellationToken cancellationToken = default)
  {
    List<Product> products =
    [
      new() { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10, WeightKg = 2.2m },
      new() { Id = 2, Name = "Smartphone", Price = 800.00m, StockQuantity = 15, WeightKg = 0.4m },
      new() { Id = 3, Name = "Wireless Mouse", Price = 25.00m, StockQuantity = 2, WeightKg = 0.1m },
      new() { Id = 4, Name = "Mechanical Keyboard", Price = 90.00m, StockQuantity = 1, WeightKg = 0.9m },
      new() { Id = 5, Name = "USB-C Hub", Price = 45.00m, StockQuantity = 30, WeightKg = 0.2m },
      new() { Id = 6, Name = "Headphones", Price = 150.00m, StockQuantity = 12, WeightKg = 0.3m },
      new() { Id = 7, Name = "27\" Monitor", Price = 350.00m, StockQuantity = 6, WeightKg = 5.0m },
      new() { Id = 8, Name = "Webcam", Price = 70.00m, StockQuantity = 20, WeightKg = 0.2m }
    ];

    await _jsonFileStore.WriteAsync(DataFiles.Products, products, cancellationToken);

    Console.WriteLine($"[Seeder] {DataFiles.Products} -> 8 records");
  }

  private async Task SeedCouponsAsync(CancellationToken cancellationToken = default)
  {
    DateTime today = DateTime.Today;

    List<Coupon> coupons =
    [
      new() { Code = "SAVE10", DiscountPercent = 10, ExpiryDate = today.AddDays(180) },
      new() { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = today.AddDays(90) },
      new() { Code = "FLASH20", DiscountPercent = 20, ExpiryDate = today.AddDays(7) },
      new() { Code = "OLD20", DiscountPercent = 20, ExpiryDate = today.AddDays(-30) }
    ];

    await _jsonFileStore.WriteAsync(DataFiles.Coupons, coupons, cancellationToken);

    Console.WriteLine($"[Seeder] {DataFiles.Coupons} -> 4 records");
  }

  private async Task SeedIncomingOrdersAsync(CancellationToken cancellationToken = default)
  {
    List<Order> orders =
    [
      new()
      {
        Id = 1,
        CustomerId = 1,
        Items = [new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 3, Quantity = 1 }],
        CouponCode = "SAVE10",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 2,
        CustomerId = 2,
        Items = [new() { ProductId = 2, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 3,
        CustomerId = 3,
        Items = [new() { ProductId = 5, Quantity = 2 }, new() { ProductId = 8, Quantity = 1 }],
        CouponCode = "WELCOME15",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 4,
        CustomerId = 4,
        Items = [new() { ProductId = 6, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 5,
        CustomerId = 5,
        Items = [new() { ProductId = 7, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 6,
        CustomerId = 6,
        Items = [new() { ProductId = 5, Quantity = 1 }],
        CouponCode = "FLASH20",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 7,
        CustomerId = 7,
        Items = [new() { ProductId = 6, Quantity = 1 }, new() { ProductId = 8, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 8,
        CustomerId = 8,
        Items = [new() { ProductId = 1, Quantity = 1 }],
        CouponCode = "SAVE10",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 9,
        CustomerId = 9,
        Items = [new() { ProductId = 3, Quantity = 5 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 10,
        CustomerId = 10,
        Items = [new() { ProductId = 4, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 11,
        CustomerId = 1,
        Items = [new() { ProductId = 4, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 12,
        CustomerId = 2,
        Items = [new() { ProductId = 2, Quantity = 1 }],
        CouponCode = "OLD20",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 13,
        CustomerId = 3,
        Items = [new() { ProductId = 6, Quantity = 2 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 14,
        CustomerId = 5,
        Items = [new() { ProductId = 8, Quantity = 2 }, new() { ProductId = 5, Quantity = 1 }],
        CouponCode = "WELCOME15",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 15,
        CustomerId = 99,
        Items = [new() { ProductId = 2, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 16,
        CustomerId = 6,
        Items = [new() { ProductId = 7, Quantity = 1 }, new() { ProductId = 5, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 17,
        CustomerId = 7,
        Items = [new() { ProductId = 2, Quantity = 1 }],
        CouponCode = "SAVE10",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 18,
        CustomerId = 8,
        Items = [new() { ProductId = 6, Quantity = 1 }],
        CouponCode = "FAKE50",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 19,
        CustomerId = 9,
        Items = [new() { ProductId = 8, Quantity = 1 }],
        CouponCode = null,
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      },
      new()
      {
        Id = 20,
        CustomerId = 10,
        Items = [new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 6, Quantity = 1 }],
        CouponCode = "FLASH20",
        SubTotal = 0m,
        Discount = 0m,
        ShippingCost = 0m,
        TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
      }
    ];

    await _jsonFileStore.WriteAsync(DataFiles.IncomingOrders, orders, cancellationToken);

    Console.WriteLine($"[Seeder] {DataFiles.IncomingOrders} -> 20 records");
  }

}