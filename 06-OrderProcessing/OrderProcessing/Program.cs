using System.Text;
using OrderProcessing.Persistence;

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

var jsonFileStore = new JsonFileStore();
var seeder = new DataSeeder(jsonFileStore);
await seeder.SeedCustomersAsync();