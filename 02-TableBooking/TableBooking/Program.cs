using TableBooking;

var booking = new FridayNightBooking();
Task[] customers = new Task[40];

for (int i = 0; i < customers.Length; i++)
{
    string name = $"Customer-{i + 1:00}";
    customers[i] = booking.BookAsync(name); // this like Task.Run(lampda or method ref)
}

// foreach (Thread t in customers)
//     t.Join();

await Task.WhenAll(customers); // when all tasks finish, resume the work form here.

Console.WriteLine();
Console.WriteLine($"Booked={booking.Booked}, TablesLeft={booking.TablesLeft}");
