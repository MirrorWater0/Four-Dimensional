# Semaphore

## Meta

- Name: Semaphore
- Source: Semaphore.xml
- Inherits: RefCounted
- Inheritance Chain: Semaphore -> RefCounted -> Object

## Brief Description

A synchronization mechanism used to control access to a shared resource by Threads.

## Description

A synchronization semaphore that can be used to synchronize multiple Threads. Initialized to zero on creation. For a binary version, see Mutex. **Warning:** Semaphores must be used carefully to avoid deadlocks. **Warning:** To guarantee that the operating system is able to perform proper cleanup (no crashes, no deadlocks), these conditions must be met: - When a Semaphore's reference count reaches zero and it is therefore destroyed, no threads must be waiting on it. - When a Thread's reference count reaches zero and it is therefore destroyed, it must not be waiting on any semaphore.

## Quick Reference

```
[methods]
post(count: int = 1) -> void
try_wait() -> bool
wait() -> void
```

## Tutorials

- [Using multiple threads]($DOCS_URL/tutorials/performance/using_multiple_threads.html)
- [Thread-safe APIs]($DOCS_URL/tutorials/performance/thread_safe_apis.html)

## Methods

- post(count: int = 1) -> void
  Lowers the Semaphore, allowing one thread in, or more if count is specified.

- try_wait() -> bool
  Like wait(), but won't block, so if the value is zero, fails immediately and returns false. If non-zero, it returns true to report success.

- wait() -> void
  Waits for the Semaphore, if its value is zero, blocks until non-zero.
