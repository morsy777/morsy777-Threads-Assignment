### Task 8.5
#### Explain in the README why Task.Run() fits here but not for the database calls?
- because the job of simulating a pdf invoice generation is a CPU bound work, so instead of the calling thread do this job, we call anothre thread from the thread bool to do this heavy operation. 

### Task 9.7 
#### when await is necessary, when fire-and-forget is acceptable (if ever), why async void is dangerous, and why it is acceptable for event handlers.
- We should use await with I/O bound to avoid thread bloking, forgetting it is acceptable if we have a heavy operation or CPU bound to run the tasks concurently.
- because we can't await an async void mehtod, and in this case if an exception occur we cannot catch it because we cant await it.
- because event handlers are required to return void by their delegate signatures in many common .NET event patterns.