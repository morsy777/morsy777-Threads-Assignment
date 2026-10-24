using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Repositories;

public class ProductRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public ProductRepository()
  {
    _jsonFileStore = new JsonFileStore();
  }

  // don't required but I need it in RaceConditionDemo instead of loading the products there
  public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    var products = await _jsonFileStore.ReadAsync<List<Product>>(DataFiles.Products, cancellationToken);

    return products ?? [];
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
      throw;
    }
    return default;
  }

  // I use the same semaphore because the Reverse & Release, both of them 
  // change the stock quantity, so I wanna avoid the interference between them.
  private readonly SemaphoreSlim _stockSemaphore = new(1, 1);

  public async Task<bool> ReserveStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
  {
    await _stockSemaphore.WaitAsync(cancellationToken);

    try
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
        throw;
      }

      if (storedProduct is null)
        return false;

      if (storedProduct.StockQuantity < quantity)
        return false;

      storedProduct.StockQuantity -= quantity;

      await _jsonFileStore.WriteAsync(DataFiles.Products, products, cancellationToken);

      return true;
    }
    finally
    {
      _stockSemaphore.Release();
    }
  }

  public async Task<bool> ReleaseStockAsync(int productId, int quantity, CancellationToken cancellationToken = default)
  {
    await _stockSemaphore.WaitAsync(cancellationToken);

    try
    {
      var products = await _jsonFileStore.ReadAsync<List<Product>>(DataFiles.Products, cancellationToken);

      Product? storedProduct = null;
      try
      {
        foreach (var product in products)
        {
          cancellationToken.ThrowIfCancellationRequested();
          if (product.Id == productId)
          {
            storedProduct = product;
            break;
          }
        }

        if (storedProduct is null)
          return false;

        if (quantity <= 0)
          return false;

        storedProduct.StockQuantity += quantity;

        await _jsonFileStore.WriteAsync(DataFiles.Products, products, cancellationToken);

        return true;
      }
      catch (OperationCanceledException)
      {
        Logger.Error<ProductRepository>("Product release was cancelled.");
        throw;
      }
    }
    finally
    {
      _stockSemaphore.Release();
    }
  }

  public async Task<bool> ReserveStockUnsafeAsync(int productId, int quantity, CancellationToken cancellationToken = default)
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
      throw;
    }

    if (storedProduct is null)
      return false;

    if (storedProduct.StockQuantity < quantity)
      return false;

    await Task.Delay(100, cancellationToken);
    storedProduct.StockQuantity -= quantity;

    await Task.Delay(400, cancellationToken);
    await _jsonFileStore.WriteAsync(DataFiles.Products, products, cancellationToken);

    return true;
  }

}
