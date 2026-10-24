namespace OrderProcessing.Models;

public class ShippingQuote
{
    public string Carrier { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long ElapsedMs { get; set; }
    public List<string> CancelledCarriers { get; set; } = [];

    public ShippingQuote() { }

    public ShippingQuote(string carrier, decimal price, long elapsedMs, List<string>? cancelledCarriers = null)
    {
        Carrier = carrier;
        Price = price;
        ElapsedMs = elapsedMs;
        CancelledCarriers = cancelledCarriers ?? [];
    }
}
