using System.Security.AccessControl;
using System.Text;
using OrderProcessing;
using OrderProcessing.Models;
using OrderProcessing.Persistence;
using OrderProcessing.DAL;

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
Logger.Error<Customer>("Test");
#endregion

var c1 = new Class1();
c1.HI();






