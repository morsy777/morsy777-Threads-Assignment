namespace InvoiceExport;

public sealed class CustomerFileStore
{
    readonly string _folder;

    public CustomerFileStore(string folder) => _folder = folder;

    public async Task<CustomerProfile> ReadProfileAsync(int customerId)
    {
        string path = Path.Combine(_folder, $"customer-{customerId}.txt");

        // Here there is a bad thing (Thread.Sleep), because it block the thread to 300ms
        // and we must reuse the thread in I/O operations to avoid resource starvation.
        // TODO: we should use async & await to avoid blocking the thread.
        // Thread.Sleep(300);
        
        // I use Task.Delay to follow the assigment but Its not usefuel here 
        // because File has an override to Async methods.
        await Task.Delay(300);
        string text = await File.ReadAllTextAsync(path);
        string[] parts = text.Split('|');
        return new CustomerProfile(int.Parse(parts[0]), parts[1], parts[2]);
    }
}
