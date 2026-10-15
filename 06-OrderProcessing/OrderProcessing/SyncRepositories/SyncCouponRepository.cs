using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.SyncRepositories;

public class SyncCouponRepository
{
  private readonly JsonFileStore _jsonFileStore;
  public SyncCouponRepository()
  {
    _jsonFileStore = new JsonFileStore();
  }

  public Coupon? GetByCode(string code)
  {
    if (string.IsNullOrWhiteSpace(code))
      return default!;

    Thread.Sleep(200);
    var coupons = _jsonFileStore.Read<List<Coupon>>(DataFiles.Coupons);

    try
    {
      Thread.Sleep(300);
      foreach (var coupon in coupons)
      {
        if (coupon.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
          return coupon;
      }
    }
    catch (OperationCanceledException)
    {
      Logger.Error<SyncCouponRepository>("Coupon search was cancelled.");
    }

    return default!;
  }
}