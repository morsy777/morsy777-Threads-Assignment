using OrderProcessing.Repositories;
using OrderProcessing.Models;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Persistence;

namespace OrderProcessing.Runners;

public class RaceConditionDemo
{
  private readonly ProductRepository _productRepository;
  private readonly JsonFileStore _jsonFileStore;
  public RaceConditionDemo()
  {
    _productRepository = new ProductRepository();
    _jsonFileStore = new JsonFileStore();
  }

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    var products = await _productRepository.GetAllAsync(cancellationToken);

    Product? keyboard = null;
    foreach (var product in products)
    {
      if (product.Id == 4)
      {
        keyboard = product;
        break;
      }
    }

    if (keyboard is null)
        return;

    keyboard.StockQuantity = 1;

    await _jsonFileStore.WriteAsync(DataFiles.Products, products, cancellationToken);

    var order10 = _productRepository.ReserveStockUnsafeAsync(4, 1, cancellationToken);
    var order11 = _productRepository.ReserveStockUnsafeAsync(4, 1, cancellationToken);

    // Using the safe virsion, one order be true and the other false with stock = 0
    // var order10 = _productRepository.ReserveStockAsync(4, 1, cancellationToken);
    // var order11 = _productRepository.ReserveStockAsync(4, 1, cancellationToken);

    var results = await Task.WhenAll(order10, order11);

    products = await _productRepository.GetAllAsync(cancellationToken);

    foreach (var product in products)
    {
      if (product.Id == 4)
      {
        keyboard = product;
        break;
      }
    }

    Console.WriteLine($"Order #10: {results[0]}");
    Console.WriteLine($"Order #11: {results[1]}");
    Console.WriteLine($"Current Stock quantity: {keyboard?.StockQuantity}");
  }
}
