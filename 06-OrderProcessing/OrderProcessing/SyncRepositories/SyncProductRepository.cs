using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;
using OrderProcessing.Repositories;

namespace OrderProcessing.SyncRepositories;

public class SyncProductRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public SyncProductRepository()
  {
    _jsonFileStore  = new JsonFileStore();
  }

  public Product? GetById(int id)
  {
    try
    {
      var products = _jsonFileStore.Read<List<Product>>(DataFiles.Products);

      foreach (var product in products)
      {
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

  public bool ReserveStock(int productId, int quantity)
  {
    Thread.Sleep(200);
    var products = _jsonFileStore.Read<List<Product>>(DataFiles.Products);

    Product? storedProduct = null;
    try
    {
      Thread.Sleep(300);
      foreach (var product in products)
      {
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
    _jsonFileStore.Write(DataFiles.Products, products);

    return true;
  }
}
