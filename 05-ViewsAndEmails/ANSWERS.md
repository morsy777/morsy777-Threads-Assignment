# Assignment 05 — Answers

Student name: Mohamed Morsi
Group / ID: #01

## Failures you observed

- There is an exception because multiple thread tries to add the same key at the same time in the dict, and these threads pass the condition because its not thread safe.

- RecordView() has two problems, not thread safe and the updates is note automic.

- There is also the same problem with queue for Email jobs, if the two different threads Deqeue the same item may be the same email is send twice for the same order.

## What you changed

- We should use Concurrent dict and also we can use locking at critical section and Interlocked to make automic update.

## Collection safety vs business-rule safety

- Collection Safety: means the collection protect its state like a ConcurrentDictionary it can protect its data structure whern multiple thread trying to access it concurrently.

- Business-rule Safety: means the app protect its business rule when mutliple request executes simultaneously, for ex: if there is a one ticket and two customer trying to buy it at the same time, the app must allow only for one cusomer to buy.
