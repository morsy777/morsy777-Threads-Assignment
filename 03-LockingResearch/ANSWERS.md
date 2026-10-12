# Assignment 03 — Answers

Student name: Mohamed Ahmed Moursy
Group / ID: Group #01

1. What is System.Threading.Monitor used for?
- is the same as Lock in C# and the Lock builded based on Monitor, we usually use monitor to lock a shared variable that must be thread safe when multiple threads trying to access it, the shared state is also called **critical section**, if more than one thread access this section at the same time, then we will have a lost update problem or inconsistency in the shared variables, for example there is a last ticket in a booking system, without monitoring the two threads can buy the same ticket, but if we use monitor, if there is a thread enter a critical section the other thread must wait until it finish and release the lock. (I wrote a linkedin post about this topic)

2. What does the C# lock statement compile to?
- its compiled into Monitor with automaticaly release of the lock, it create a lock on the object, so if a thread try to enter the critical section/ shared state and there is another thread already in it, the thread must wait until the first thread finish.

3. List at least two differences between using lock and calling Monitor APIs directly?
- as I mentioned in Q2, the lock automaticaly release the lock when the execution finsih or the thread leave the critical section even an exception occur, but the Monitor we must try/finally to release the lock and also provides TryEnter, Wait, and Pluse to give us more control over waiting behaviors.
- So Lock provides simpler and safer execution and Monitor offer more control over sync and waiting behaviors.

4. When might a developer use Monitor.TryEnter, Monitor.Pulse, or Monitor.Wait instead of a plain
lock?

- when we need a time out and threads wait for a condition to notify each other, this give us more control over sync.

- TryEnter: the thread wait and try to acquire a lock, but it stop waiting after a specified time if the lock is unavaliable, we use it in something like rate limit in API, for ex after a specified period return request time out.

- Pulse: when the lock released, it notify one thread from the waiting threads for that lock.

- Wait: the thread release lock and wait until receive a notification from the working thread when it release the lock, and then the thread try to acquire the lock.

5. Write the same critical section twice: once with lock, once with Monitor.Enter / Monitor.Exit.
### 1. Using `lock` with `System.Threading.Lock`

```csharp
private readonly Lock _lock = new();

lock (_lock)
{
    // Critical section 

    // Automatically release
}
```

### 2. Using `Monitor.Enter()` / `Monitor.Exit()`

```csharp
private readonly Lock _lock = new();

Monitor.Enter(_lock);

try
{
    // Critical section
}
finally
{
    Monitor.Exit(_lock); // release 
}
```




Links:

- [C# Corner](https://www.c-sharpcorner.com/article/introduction-to-monitor-class-in-c-sharp)
- [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor.tryenter?view=net-10.0)
