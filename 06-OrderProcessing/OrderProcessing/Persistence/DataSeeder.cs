using System.Text;
using System.Text.Json;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence.Constants;

namespace OrderProcessing.Persistence;

// I use primary ctor to inject the JsonFileStore 
public class DataSeeder(JsonFileStore jsonFileStore)
{
    private readonly JsonFileStore _jsonFileStore = jsonFileStore;

    public async Task SeedCustomersAsync(CancellationToken cancellationToken = default)
    {
        List<Customer> customers =
        [
            new() { Id = 1, Name = "Ahmed Ali", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 2, Name = "Sara Mohamed", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 3, Name = "Omar Hassan", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 4, Name = "Mona Ibrahim", IsActive = false, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 5, Name = "Youssef Adel", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 6, Name = "Nour Khaled", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 7, Name = "Karim Samir", IsActive = true, LoyaltyTier = LoyaltyTier.Silver },
            new() { Id = 8, Name = "Laila Mahmoud", IsActive = true, LoyaltyTier = LoyaltyTier.Gold },
            new() { Id = 9, Name = "Hassan Tarek", IsActive = true, LoyaltyTier = LoyaltyTier.Bronze },
            new() { Id = 10, Name = "Dina Fathy", IsActive = true, LoyaltyTier = LoyaltyTier.Silver }
        ];

        await _jsonFileStore.WriteAsync(DataFiles.Customers, customers, cancellationToken);
    }

}