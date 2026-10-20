### Task 8.5
#### Explain in the README why Task.Run() fits here but not for the database calls?
- because the job of simulating a pdf invoice generation is a CPU bound work, so instead of the calling thread do this job, we call anothre thread from the thread bool to do this heavy operation. 