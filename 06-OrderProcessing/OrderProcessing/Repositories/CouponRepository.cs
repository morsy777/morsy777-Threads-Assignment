using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Repositories;

public class CouponRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public CouponRepository()
  {
    _jsonFileStore = new JsonFileStore();
  }

  public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(code))
      return default!;

    await Task.Delay(200, cancellationToken);
    var coupons = await _jsonFileStore.ReadAsync<List<Coupon>>(DataFiles.Coupons, cancellationToken);

    try
    {
      await Task.Delay(300, cancellationToken);
      foreach (var coupon in coupons)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (coupon.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
          return coupon;
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<CouponRepository>("Coupon search was cancelled.");
    }

    return default!;
  }
}
