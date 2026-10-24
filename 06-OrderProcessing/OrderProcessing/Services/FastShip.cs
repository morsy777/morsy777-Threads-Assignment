using OrderProcessing.Repositories;

namespace OrderProcessing.Services;

public class FastShip : BaseShippingCarrier
{
    public override string Name => "FastShip";
    public override decimal PriceFactor => 1.00m;
    public override int MinDelayMs => 500;
    public override int MaxDelayMs => 1500;

    public FastShip(IEnumerable<int>? failForOrderIds = null, ProductRepository? productRepository = null)
        : base(failForOrderIds, productRepository)
    {
    }
}
