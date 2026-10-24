# Assignment 06 — Answers

Student name: Mohamed Morsi
Group / ID: #01


## Phase 1 questions:

### 1- Why are the sync and async times almost the same when orders are awaited one after another?
- First thing async doesn't block the thread while waiting for async operation, this the main deference between it and sync programing, but there is no relation between the execution time and async, so it doesnt make the code executes faster + the thread.sleep is converted into task.daly.

### 2- Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence?
- May be yes, May be no, it depends on the thread that will come after the await from the thread pool.
- because when the thread face an await it come back to the thread bool to handle other request, after the await, any thread available in the thread pool will consume the execution of the method, so **we can't guarantee** is the thread that start the function execution will consume it.
- When I log message & current thread id and then using (await Task.Delay(200)) after the await When I log a gain with different color, then the thread id doesn't change because I use sync log with lock, if I decide in the feauture to use async lock, then I should use semaphoreSlim, because in Lock, the thread that aquire the lock must release the lock and if we use (async/await) we can't guarantee the smae thread that aquire is the same that come back to release because ASP.Net core doesnt support synconarization context like wpf.

### 3- What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?
- Task doesn't block the thread and istead of that, it return it to the thread bool to handle another request.
- Thread it block the thread until the operation finsih, this is the bad thing because the thread remain ideal, doesnt do anything.

### 4- Why should a repository method return Task<T> instead of T?
- Because it reads/wrties files so it should allow async operation without blocking the calling thread.
- We should use async in operations such as Files, I/O bound, DB calls and external API cals, to avoid thread starvation.

### 5- What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?
- The main difference is about blocking the calling thread. File.ReadAllText blocks the calling thread so, it remain idel without doing anything. File.ReadAllTextAsync returns the calling thread into the thread bool insted of blocking it.
- No, the async version doesnt occupy thread in reading file, if we use await.

## Phase 2 questions:
### 1- What is the difference between starting a task and awaiting it?
- The main difference is about parallel execution & concurrent execution.
- Starting the task makes a parallel execution with other tasks that started at the same moment, but awaiting it mean the execution done in a concurrent way.
- We usually statrting the **eavy tasks liks CPU bound for example report generation in ourtask**, but **I/O, DB calls, Http requests we usually use await them**.
- In other word, Starting the task means the task is being executed sync until the first await in the method, but awaiting it means the calling thread will back into the pool to do another work until the task finish its execution.


### 2- Does every Task create a new thread? Does Task.Delay() use a thread while waiting?
- Task doesn't create a new thread, also It doesn't block the caller thread.
- No, Task.Delay doesn't occupy a thread while waiting, It allows the caller thread to return to the pool and do another work instead of blocking.

### 3- What happens if you call .Result or .Wait() on an unfinished task instead of await? Why is it dangerous? Write a small separate demo method — the only place .Result / .Wait() is allowed.

- It occupy the caller thread until the task finishes.
- Because it blocks the caller thread, that's mean the thread stay waiting without doing anything and this lead to thread starvation.

```csharp
static void BlockingDemo()
{
  Task task = Task.Delay(2000);

  task.Wait(); // Blocks until the task finishes

  Console.WriteLine("Done!");
}
```

### 4 - What happens to an exception thrown inside a task that is never awaited?
- The exception will stored inside the task, and we can't observet it.

### 5- When should you use Task.Run(), and when should you avoid it?
- When I have a heavy CPU bound operation such as generating a report, and avoid it for call externall service, DB or I/O bound.

### 6- What is the difference between Task and Task<T>?
- the main difference is the return type.
- Task doesn't return a result while Task<T> returns a reult of type t.

### 7- Why can a lock block not contain an await?
- because the thread that aquires a lock must release it, when we using await we can't guarante the same thread that awaited will returned a gain to consume the work and release the lock.

### 8- Why did the unprotected stock reservation oversell, and how did you prevent it?
- because two orders tried to reverse the last uint of the same product at the same time, and there was no lock on stock reservation, I prevent it by using a semaphore slim to allow only one order at a time to reverse the stock.

## Phase 3 Questions:

```csharp
public async Task ProcessOrderAsync(Order order, Customer customer)
{
    await _orderRepository.SaveAsync(order);
    _emailService.SendConfirmationAsync(order, customer);
    Console.WriteLine("Order processing finished");
}
```
### 1 Find the problem in this code?
- if an exception occur in SendConfirmationAsync, the excpetion will stored inside the task and the ProcessOrderAsync will countinue its work and print "Order processing finished", whichs mislead the users into thinking the entire process has successeded, even though the confirmation email might not have been sent yet.

### 2 Show what happens if the email is still being sent when the method returns, or when the console app exits?
- The email might not be sent if an exception occurs. Also, if the console app exits before the email-sending task completes, the email might not be sent because the process terminates.

### 3- Show what happens if the email operation throws an exception?
- The exception will stored in the returned task, so ProcessOrderAsync didn't observe anything.

### 4- Fix the implementation. Show the console output before and after the fix.
- Just use an 'await' keyword, if an exception occured, the ProcessOrderAsync will observe it so it doesn't print "Order processing finished".

## Phase 4 Questions:
### 1- For each of Tasks 10.1–10.4: which tool did you use, and why can't "start, then await one by one" meet the requirement?
- I used Task.WhenAny(), because if I started all 20 orders at the same time, some orders may finish earlier than others, so instead of waiting for the previous orders to finish, I can handle each order as soon as completed.

### 2- In 10.2, when several loads fail, how do you get every exception? What does a plain await give you?
- We can get all exceptions from Task.WhenAll using allTasks.Exception.Flatten().InnerExceptions inside the catch block.

- Plain await throws one exception to the catch block, but the Task.WhenAll task stores all the exceptions.

### 3- In 10.3, what happens to the losing carriers if you never cancel them?
- If we never cancel the losing carriers, they will continue running even after we get the fastest successful quote. This wastes resources and may increase unnecessary work.

### 4- Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?
- Part A gets faster with more CPU cores because the CPU can process multiple scoring calculations in parallel.

- Part B does not depend directly on the number of CPU cores because sending emails is I/O-bound, which means most of the time is spent waiting.

### 5- What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?
- Parallel work means executing multiple pieces of work at the same time using multiple CPU cores. It is suitable for CPU-bound tasks.

- Concurrent asynchronous work means starting multiple operations that can make progress while others are waiting, without blocking a thread unnecessarily. It is suitable for I/O-bound tasks.

## Questions in Tasks:

### Task 8.5
#### Explain in the README why Task.Run() fits here but not for the database calls?
- because the job of simulating a pdf invoice generation is a CPU bound work, so instead of the calling thread do this job, we call anothre thread from the thread bool to do this heavy operation. 

### Task 9.6
#### 1. Does async make one order's workflow faster?
- No, async does not necessarily make one order faster. It helps the application use its threads more efficiently while waiting for I/O operations.

#### 2. Why does async scale better for many orders?
- Async scales better because threads are not blocked while waiting for I/O operations. They can be used to handle other work.

#### 3. What happens to the sync version with 1,000 orders?
- The sync version may need many threads, consume more resources, and become slower or run out of available threads under heavy load.
#### 4. Why is async essential for apps that call databases, files and APIs?
- Because these operations often spend time waiting. Async allows the application to handle other requests during that time, improving scalability and responsiveness.

### Task 9.7 
#### when await is necessary, when fire-and-forget is acceptable (if ever), why async void is dangerous, and why it is acceptable for event handlers.
- We should use await with I/O bound to avoid thread bloking, forgetting it is acceptable if we have a heavy operation or CPU bound to run the tasks concurently.
- because we can't await an async void mehtod, and in this case if an exception occur we cannot catch it because we cant await it.
- because event handlers are required to return void by their delegate signatures in many common .NET event patterns.
