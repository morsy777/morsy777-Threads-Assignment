using System.Text.Json;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class ReportGenerator
{
  private readonly OrderRepository _orderRepository;
  private readonly ProductRepository _productRepository;
  private readonly string _reportsDirectory;

  public ReportGenerator()
  {
    _orderRepository = new OrderRepository();
    _productRepository = new ProductRepository();
    _reportsDirectory = Path.Combine(AppContext.BaseDirectory, DataFiles.DataDir, DataFiles.ReportsDir);
  }

  public async Task<Report> GenerateAsync(CancellationToken cancellationToken = default)
  {
    var orders = await _orderRepository.GetAllAsync(cancellationToken);

    int totalOrders = orders.Count;

    int completedCount = 0;
    int rejectedCount = 0;
    int failedCount = 0;
    int paymentFailedCount = 0;
    decimal totalRevenue = 0m;

    var productQuantities = new Dictionary<int, int>();

    foreach (var order in orders)
    {
      switch (order.Status)
      {
        case OrderStatus.Completed:
          CountCompletedOrders(order, ref completedCount, ref totalRevenue, productQuantities);
          break;

        case OrderStatus.Rejected:
          CountRejectedOrders(ref rejectedCount);
          break;

        case OrderStatus.Failed:
          CountFailedOrders(ref failedCount);
          break;

        case OrderStatus.PaymentFailed:
          CountPaymentFailedOrders(ref paymentFailedCount);
          break;
      }
    }

    decimal averageOrderValue = completedCount > 0 ? totalRevenue / completedCount : 0m;

    int topProductId = 0;
    int topQuantity = 0;

    foreach (var kvp in productQuantities)
    {
      int productId = kvp.Key;
      int quantity = kvp.Value;

      if (quantity > topQuantity)
      {
        topQuantity = quantity;
        topProductId = productId;
      }
      else if (quantity == topQuantity && (topProductId == 0 || productId < topProductId))
      {
        topProductId = productId;
      }
    }

    string topProductName = string.Empty;
    if (topProductId > 0)
    {
      var product = await _productRepository.GetByIdAsync(topProductId, cancellationToken);
      topProductName = product?.Name ?? string.Empty;
    }

    var report = new Report
    {
      TotalOrders = totalOrders,
      TotalRevenue = totalRevenue,
      CompletedCount = completedCount,
      RejectedCount = rejectedCount,
      FailedCount = failedCount,
      PaymentFailedCount = paymentFailedCount,
      TopSellingProduct = topProductName,
      TopSellingQuantity = topQuantity,
      AverageOrderValue = averageOrderValue,
      GeneratedAt = DateTime.Now
    };

    if (!Directory.Exists(_reportsDirectory))
      Directory.CreateDirectory(_reportsDirectory);

    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string fileName = string.Format(DataFiles.ReportFilePattern, timestamp);
    string filePath = Path.Combine(_reportsDirectory, fileName);

    string json = JsonSerializer.Serialize(report, JsonSerializerConfig.Options);
    await File.WriteAllTextAsync(filePath, json, cancellationToken);

    return report;
  }

  private static void CountCompletedOrders(Order order, ref int completedCount, ref decimal totalRevenue, Dictionary<int, int> productQuantities)
  {
    completedCount++;
    totalRevenue += order.TotalAmount;

    foreach (var item in order.Items)
    {
      if (!productQuantities.ContainsKey(item.ProductId))
      {
        productQuantities[item.ProductId] = 0;
      }
      productQuantities[item.ProductId] += item.Quantity;
    }
  }

  private static void CountRejectedOrders(ref int rejectedCount) => rejectedCount++;
  private static void CountFailedOrders(ref int failedCount) => failedCount++;
  private static void CountPaymentFailedOrders(ref int paymentFailedCount) => paymentFailedCount++;
}
