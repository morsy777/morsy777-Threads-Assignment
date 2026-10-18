# Assignment 06 — Answers

Student name: Mohamed Morsi
Group / ID: #01

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