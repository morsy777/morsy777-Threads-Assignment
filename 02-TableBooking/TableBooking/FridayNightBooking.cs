namespace TableBooking;

public sealed class FridayNightBooking
{
    public int TablesLeft { get; private set; } = 5;
    public int Booked { get; private set; }

    // new(1,1):
    //  permit for 1 thread, and max permits is 1.
    private readonly SemaphoreSlim  _semaphore = new(1,1); 
    public async Task BookAsync(string customerName)
    { 
        // only one thread can enter the lock, if another thread come,
        // will wait until release the lock.
        await _semaphore.WaitAsync();

        try
        {
          if (TablesLeft <= 0)
            return;

          await Task.Delay(20);

          TablesLeft--;
          Booked++;
        }
        finally
        {
          _semaphore.Release(); // after finishing the work release the lock (unlock)
        }
        Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
    }
}
