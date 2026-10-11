namespace InvoiceExport;

public sealed class InvoiceFileWriter
{
    readonly string _folder;

    public InvoiceFileWriter(string folder) => _folder = folder;

    public async Task<string> WriteInvoiceAsync(Order order, CustomerProfile customer)
    {
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, $"invoice-{order.Id}.txt");

        string body =
            $"""
            SwiftBite Invoice
            -----------------
            Order:      {order.Id}
            Restaurant: {order.Restaurant}
            Customer:   {customer.Name} <{customer.Email}>
            Total:      {order.Total:C}
            """;
        
        // I hate this Thread.Sleep, because it block the thread to 300ms 
        // so it makes resource starvation. I should repalce it with async & await.
        // for exp if we have 1000 request, in this case we will block 1000 threads 
        // for 300ms, so it make resource starvation.
        // Thread.Sleep(300);

        await Task.Delay(300);
        await File.WriteAllTextAsync(path, body);
        return path;
    }
}
