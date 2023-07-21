# CoreEx.Invokers

The `CoreEx.Invokers` namespace provides invocation capabilities.

<br/>

## Motivation

To enable a standardized approach to the invocation of logic enabling a means to add surrounding runtime execution logic decoupled from the initial implementation; enabling additional functionality to be added separately where desired (i.e. logging).

By default the invoke represents a standardized [tracing/instrumentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs#add-basic-instrumentation) boundary.

<br/>

## Base capabilities

The [`InvokerBase<TInvoker>`](./InvokerBaseT.cs) and [`InvokerBase<TInvoker, TArgs>`](./InvokerBaseT2.cs) provide the base functionality to invoke an underlying `Action` or `Func` either synchronously or asynchronously. The virtual `OnInvoke` (synchronous) and `OnInvokeAsync` (asynchronous) methods must be overridden to extend the functionality.

<br/>

### Instrumentation

Internally an [`InvokeArgs`](./InvokeArgs.cs) is created to provide the context for the invocation. This is passed to the `Invoke` or `InvokeAsync` methods and provides access to an underlying [`Activity`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) property.

By default where tracing is enable the following standard properties are recorded:

Property | Description
-|-
`invoker.type` | The .NET type name of the invoker.
`invoker.owner` | The .NET type name of the invoker owner/caller.
`invoker.member` | The .NET member name of the invoker.
`invoker.result` | The result of the invocation. Where the result is an [`IResult`](../Results/IResult.cs) (see [Railway-oriented programming](../Results/README.md)) will be either `Success` or `Failure`; otherwise `Complete`. An unhandled exception will be `Exception` (not that the underlying exception details are not recorded).
`invoker.failure` | Where the result is an `IResult` and its state is `IsFailure` then corresponding `Error.Message` is recorded.

Additional tracing properties may be included where these specifically have been added.

<br/>

### Usage

The general usage pattern is to provide a concrete implementation, for example [`DatabaseInvoker`](../../CoreEx.Database/DatabaseInvoker.cs) and then leverage within application to invoke (wrap) key logic. To then enable runtime overridding, the owning class would allow an instance to be provided within the constructor (i.e. Dependency Injection), for example [`Database`](../../CoreEx.Database/Database.cs); or provide a static instance (i.e. Singleton), for example [`ValidationInvoker`](../Validation/ValidationInvoker.cs) via the `Current` property.

<br/>

## Business logic invoker

The [`InvokerBase`](./InvokerBase.cs) and corresponding [`InvokerArgs`](./InvokerArgs.cs) provides standardized (common) business services logic (leveraged where applicable).

1. Copy the [`OperationType`](../OperationType.cs) from `ExecutionContext.Current`; and reset after execution.
2. Override the `ExecutionContext.Current` when `InvokerArgs.OperationType` is specified.
3. Initiate a tranasction ([TransactionScope](https://learn.microsoft.com/en-us/dotnet/api/system.transactions.transactionscope)) when `InvokerArgs.IncludeTransactionScope` is specified.
4. **Invoke** the underlying `Action` or `Func` (overrides virtual `OnInvokeAsync` method).
5. Send any events (see [`IEventPublisher.SendAsync`](../Events/IEventPublisher.cs)) when `InvokerArgs.EventPublisher` is specified.
6. Complete the tranasction ([TransactionScope](https://learn.microsoft.com/en-us/dotnet/api/system.transactions.transactionscope)) where previously initiated.
7. On **`Exception`** invoke the `InvokerArgs.ExceptionHandler` where specified.

The [`ManagerInvoker`](./ManagerInvoker.cs), [`DataSvcInvoker`](./DataSvcInvoker.cs) and [`DataInvoker`](./DataInvoker.cs) provide implementations of the `InvokerBase` that enable usage within these named common business services logic layers where applicable.

<br/>

## Invoker

The [`Invoker`](./Invoker.cs) provides the following common functions.

Method | Description
-|-
`InvokeAsync` | Invokes the passed `Task` where not `null`; otherwise returns `Task.CompletedTask`. This is useful to invoke a `Task` where it is not known until runtime whether it is `null` or not, encapsulating the conditional logic.
`RunSync` | Runs (invokes) the passed `Task` synchronously. The general guidance is to avoid sync over async as this may result in deadlock, so please consider all options before using. There are many [articles]("https://stackoverflow.com/questions/5095183/how-would-i-run-an-async-taskt-method-synchronously") written discussing this subject; however, if sync over async is needed this method provides a consistent approach to perform. This implementation has been inspired by https://www.ryadel.com/en/asyncutil-c-helper-class-async-method-sync-result-wait/".


