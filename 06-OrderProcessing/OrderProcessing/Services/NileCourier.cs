using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class NileCourier : BaseShippingCarrier
{
    public override string Name => "NileCourier";
    public override decimal PriceFactor => 1.10m;
    public override int MinDelayMs => 800;
    public override int MaxDelayMs => 1200;

    public NileCourier(IEnumerable<int>? failForOrderIds = null, ProductRepository? productRepository = null)
        : base(failForOrderIds, productRepository)
    {
    }
}
