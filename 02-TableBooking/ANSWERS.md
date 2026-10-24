# Assignment 02 — Answers

Student name: Mohamed Ahmed Morsy
Group / ID: Group #01

## What is wrong

- As a business 50 customer bock 5 tables ??! yes the same table is assigned to multiple customer at the same time due to thread interleaving and also the value of TablesLeft is readed, updated then written and this not automic so there is a lost update problem, so to solve is we must make this process **automic** which mean make all things or no things.

- The problme is: there is no thread saftey(on the condition) & there is automicity at Increment and Decrement.

- The Book() is not thread safe, because if two thread read the condition at the same time, will read the same value of TablesLeft and both will decrement it and therefore one of change will be lost + TablesLeft will decrese to be a negative and this not allowable.

## Solution I try it

- I search for that error so we can't use await inside lock, because it pause the method and realse the thread until I/O finish, but after I/O finsih we can't graunte the thread will resume the work, is the same thread that start the work and make a lock. So, I will use async lock such as semaphor slim.

- The rule: the thread that lock critical section must unlock it, and if we use awiat, may be another thread will come back to countinue a work but this not allow because this thread can't unlock the critical secation. Only the thread locked it can unlock.

```csharp
public async Task Book(string customerName)
{
    lock (_lock)
    {
        if (TablesLeft <= 0)
            return;

        await Task.Delay(20); // compile error

        TablesLeft--;
        Booked++;
    }
}
```


## How you fixed it

- First thing we must save the threads so I should use async & await insted of Thread.Sleep, then we must make a locking on the condition and shared state (TablesLeft, Booked) to make the code thread safty and automic without in between thread.
