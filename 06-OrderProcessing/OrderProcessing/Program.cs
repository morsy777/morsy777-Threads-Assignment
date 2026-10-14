using System.Security.AccessControl;
using System.Text;
using OrderProcessing;
using OrderProcessing.Models;
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

var repo = new CustomerRepository();
var customer = await repo.GetByIdAsync(1);
Console.WriteLine(customer?.Name);





