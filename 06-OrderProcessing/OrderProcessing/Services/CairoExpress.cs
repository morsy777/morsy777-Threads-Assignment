using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class CairoExpress : BaseShippingCarrier
{
    public override string Name => "CairoExpress";
    public override decimal PriceFactor => 0.95m;
    public override int MinDelayMs => 300;
    public override int MaxDelayMs => 2000;

    public CairoExpress(IEnumerable<int>? failForOrderIds = null, ProductRepository? productRepository = null)
        : base(failForOrderIds, productRepository)
    {
    }

    protected override bool ShouldFail(int orderId)
    {
        return orderId % 2 == 0 || base.ShouldFail(orderId);
    }
}
