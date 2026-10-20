using OrderProcessing.Models;
using OrderProcessing.Persistence.Constants;
using System.Text;

namespace OrderProcessing.Services;

public class InvoiceGenerator
{
  private readonly string _invoicesDirectory;

  public InvoiceGenerator()
  {
    _invoicesDirectory = Path.Combine(AppContext.BaseDirectory, DataFiles.DataDir, DataFiles.InvoicesDir);
  }

  public async Task GenerateAsync(Order order, CancellationToken cancellationToken)
  {
    if (order is null)
      return;

    string invoiceContent = await Task.Run(() => RenderInvoice(order), cancellationToken);


    if (!Directory.Exists(_invoicesDirectory))
      Directory.CreateDirectory(_invoicesDirectory);

    string fileName = $"invoice_{order.Id}.txt";
    string filePath = Path.Combine(_invoicesDirectory, fileName);

    await File.WriteAllTextAsync(filePath, invoiceContent, cancellationToken);
  }

  private static string RenderInvoice(Order order)
  {
    long count = 0;
    for (int i = 0; i < 20_000_000; i++)
    {
      count++;
    }

    var sb = new StringBuilder();
    sb.AppendLine("==========================================");
    sb.AppendLine("                 INVOICE                  ");
    sb.AppendLine("==========================================");
    sb.AppendLine($"Order ID: {order.Id}");
    sb.AppendLine($"Customer ID: {order.CustomerId}");
    sb.AppendLine($"Date: {order.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine("Status: Completed");

    if (!string.IsNullOrWhiteSpace(order.CouponCode))
      sb.AppendLine($"Coupon Code: {order.CouponCode}");

    sb.AppendLine("------------------------------------------");
    sb.AppendLine("Items:");
    foreach (var item in order.Items)
    {
      sb.AppendLine($" - Product #{item.ProductId} | Quantity: {item.Quantity} | Unit Price: {item.UnitPrice:F2} | Line Total: {(item.Quantity * item.UnitPrice):F2}");
    }

    sb.AppendLine("------------------------------------------");
    sb.AppendLine($"SubTotal: {order.SubTotal:F2}");
    sb.AppendLine($"Discount: {order.Discount:F2}");
    sb.AppendLine($"Shipping Cost: {order.ShippingCost:F2}");
    sb.AppendLine($"Total Amount: {order.TotalAmount:F2}");
    sb.AppendLine("==========================================");

    if (count < 0)
      sb.AppendLine(count.ToString());

    return sb.ToString();
  }
}

