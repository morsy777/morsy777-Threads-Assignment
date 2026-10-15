using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Repositories;

public class ProductRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public ProductRepository()
  {
    _jsonFileStore  = new JsonFileStore();
  }

  public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    try
    {
      var products = await _jsonFileStore.ReadAsync<List<Product>>(DataFiles.Products, cancellationToken);

      foreach (var product in products)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (product.Id == id)
          return product;
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<ProductRepository>("Product search was cancelled.");
    }
    return default;
  }


  // I try to reuse GetByIdAsync but it returns a product and we must 
  // rewrite the all products after each modification because, wrtieAsync in
  // JsonFileStore, overwrite the data, by creating a tempFile that overwrite
  // the original file (product.json)
  public async Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
  {
    await Task.Delay(200, cancellationToken);
    var products = await _jsonFileStore.ReadAsync<List<Product>>(DataFiles.Products, cancellationToken);

    Product? storedProduct = null;
    try
    {
      await Task.Delay(300, cancellationToken);
      foreach (var product in products)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (product.Id == productId)
        {
          storedProduct = product;
          break;
        }
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<ProductRepository>("Product reserve was cancelled.");
      return false;
    }

    if (storedProduct is null)
      return false;

    if (storedProduct.StockQuantity < quantity)
      return false;

    storedProduct.StockQuantity -= quantity;
    await _jsonFileStore.WriteAsync(DataFiles.Products, products, cancellationToken);

    return true;
  }
}
