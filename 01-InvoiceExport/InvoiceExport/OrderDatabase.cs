namespace InvoiceExport;

public sealed class OrderDatabase
{
    readonly List<Order> _pending =
    [
        new(1001, 1, 240.00m, "Zooba"),
        new(1002, 2, 180.50m, "Koshary El Tahrir"),
        new(1003, 3, 95.00m, "Hawawshi House"),
        new(1004, 1, 310.00m, "Zooba"),
        new(1005, 4, 150.00m, "Fattah Street"),
    ];

    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        //Thread.Sleep(800);
        await Task.Delay(800);
        // we can't convert it to listAsync because it stored in memory 
        // as List and converting it to listAsync doesn't require I/O.
        return _pending.ToList(); 
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        //Thread.Sleep(400);
        await Task.Delay(400);
        return _pending.FirstOrDefault(o => o.Id == id);
    }
}
