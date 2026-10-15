using System.Security.AccessControl;
using System.Text;
using OrderProcessing;
using OrderProcessing.Models;
using OrderProcessing.Models.Enums;
using OrderProcessing.Persistence;
using OrderProcessing.Repositories;

#region Test Email 
// string Name = "Mohamed Morsi";
// string EmailBuilder()
// {
//   var names = Name.Trim().Split(' ');
//   var sb = new StringBuilder();

//   for (int i = 0; i < names.Length; i++)
//   {
//     names[i] = names[i].ToLowerInvariant(); // to ignore computer culture like Turky
//   }

//   sb.AppendJoin('.', names);
//   sb.Append("@gmail.com");

//   return sb.ToString();
// }

// Console.WriteLine(EmailBuilder());
#endregion

#region Test Seeding
// var jsonFileStore = new JsonFileStore();
// var seeder = new DataSeeder(jsonFileStore);
// await seeder.SeedAsync();
# endregion

#region Test Logging
// Logger.Success<Customer>("Test");
// Logger.Info<Customer>("Test");
// Logger.Error<Customer>("Test");
#endregion

var cusomerRepo = new CustomerRepository();
var customer = await cusomerRepo.GetByIdAsync(1);
Console.WriteLine(customer?.Name);

var productRepo = new ProductRepository();
var product = await productRepo.GetByIdAsync(1);
Console.WriteLine(product?.Name);

// Console.WriteLine(await productRepo.ReserveStockAsync(1, 5));

var couponRepo = new CouponRepository();
var coupon = await couponRepo.GetByCodeAsync("SAVE10");
Console.WriteLine(coupon?.DiscountPercent);

var orderRepo = new OrderRepository();
var updatedOrder = new Order
{
  Id = 1,
  CustomerId = 1,
  Items =
  [
    new OrderItem
    {
      ProductId = 1,
      Quantity = 1,
      UnitPrice = 50
    },
    new OrderItem
    {
      ProductId = 3,
      Quantity = 1,
      UnitPrice = 50
    }
  ],
  CouponCode = "SAVE10",
  SubTotal = 300,
  Discount = 30,
  ShippingCost = 40,
  TotalAmount = 310,
  Status = OrderStatus.Completed,
  CreatedAt = DateTime.Now,
  FailureReason = null,
  TransactionId = null
};

// await orderRepo.SaveAsync(updatedOrder);

var orders = await orderRepo.GetAllAsync();
foreach (var order in orders)
{
  Console.WriteLine($"Order: {order.Id}");
  Console.WriteLine($"Customer: {order.CustomerId}");
  foreach (var item in order.Items)
  {
    Console.WriteLine
      ($"Product: {item.ProductId} || Quantity: {item.Quantity} || Price: {item.UnitPrice}");
  }
}

// var filteredOrders = await orderRepo.GetByStatusAsync(OrderStatus.Completed);
var filteredOrders = await orderRepo.GetByStatusAsync(OrderStatus.Pending);
foreach (var order in filteredOrders)
{
  Console.WriteLine($"Order: {order.Id}");
  Console.WriteLine($"Customer: {order.CustomerId}");
}


