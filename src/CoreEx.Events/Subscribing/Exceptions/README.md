# CoreEx.Events.Subscribing.Exceptions

> Defines the subscriber-specific exception types raised by the `ErrorHandling` pipeline; each exception carries the `ErrorHandling` value that triggered it, allowing host infrastructure to inspect and act on the outcome.

## Overview

When the `ErrorHandler` resolves an error outcome of `None`, `DeadLetter`, `Catastrophic`, or `Retry`, the subscribing pipeline translates that outcome into a typed exception that propagates back to the transport host. The host can catch the specific exception type to determine the correct broker-level action — abandoning, dead-lettering, completing, or retrying the message — without inspecting raw exception messages.

All exceptions in this namespace implement `IEventSubscriberException`, which exposes the `ErrorHandling` value that produced the exception, and most wrap the original inner exception for full stack-trace preservation.

## Key types

| Type | Description |
|------|-------------|
| [`IEventSubscriberException`](./IEventSubscriberException.cs) | Marker interface carried by all subscriber exceptions; exposes the `ErrorHandling` value that triggered the exception. |
| **[`EventSubscriberUnhandledException`](./EventSubscriberUnhandledException.cs)** | Raised when `ErrorHandling.None` is resolved; bubbles the unhandled error to the host process for broker-level handling. |
| **[`EventSubscriberCatastrophicException`](./EventSubscriberCatastrophicException.cs)** | Raised when `ErrorHandling.Catastrophic` is resolved; signals a fatal, non-retryable condition that the host should escalate. |
| **[`EventSubscriberDeadLetterException`](./EventSubscriberDeadLetterException.cs)** | Raised when `ErrorHandling.DeadLetter` is resolved; instructs the host to forward the message to the dead-letter destination. |
| **[`EventSubscriberRetryException`](./EventSubscriberRetryException.cs)** | Raised when `ErrorHandling.Retry` is resolved; instructs the host to re-enqueue or abandon the message for retry. |
| **[`EventSubscriberHandledException`](./EventSubscriberHandledException.cs)** | Raised for `CompleteAs*` outcomes where logging has occurred; signals the host to complete the message without further escalation. |
| **[`EventSubscriberReceiveException`](./EventSubscriberReceiveException.cs)** | Wraps an error that occurred during the raw receive/deserialization phase before handler dispatch. |

## Related namespaces

- **[`CoreEx.Events.Subscribing`](../README.md)** - Parent namespace; `ErrorHandler` and `EventSubscriberBase` produce and consume these exception types.