using System.Text;
using OrderProcessing.Models.Enums;

namespace OrderProcessing.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; set; }
    public LoyaltyTier LoyaltyTier { get; set; }

    public Customer() { }

    public void GenerateEmail()
    {
      var names = Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
      var sb = new StringBuilder();

      for (int i = 0; i < names.Length; i++)
        names[i] = names[i].ToLowerInvariant(); // to ignore computer culture like Turky

      sb.AppendJoin('.', names);
      sb.Append("@gmail.com");

      Email = sb.ToString();
    }
}