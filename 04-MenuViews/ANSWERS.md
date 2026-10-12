# Assignment 04 — Answers

Student name: Mohamed Ahmed Morsi
Group / ID: Group #01

## Why Expected and Actual disagree

- because there is a lost update problem, its a pobular problem in OS and threads interleaving, so to handle this problem all updates must be automic, instead of doing read, update, write and there is a probability of two different threads read the same value and update it, we can use automic update, the concpet of automicity is do all thing or no thing.

## What you changed

- I just use automic increment.

## When would you protect the update differently

- when there is a mutliple thread can change shared state like Views in our example.
