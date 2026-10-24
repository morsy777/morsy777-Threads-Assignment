# Assignment 01 — Answers

Student name: Mohamed Ahmed Morsy
Group / ID: Group #01

## Problems with the current design

- realy there is a bad design because it block the thread for a Nms in every I/O operation, so if we have 1000 request, in this case we will block 1000 threads for Nms, so it make resource starvation,
because the thread is blocked and doesn't do any other work, the right thing is to reuse the thread to preseve system resources, I sholud replace this with async & await to avoid blocking the thread.

## What you changed

- Every I/O operation that block the thread for Nms, I replaced it with async & await to avoid blocking the thread and reuse it to preseve system resources.

- Using async & await, if we have 1000 request, in this case we will reuse the thread to do other work and avoid resource starvation.

- resource starvation: Its mean blocking the thread so it doesn't do any other work, I was studing it in OS & DB2.
