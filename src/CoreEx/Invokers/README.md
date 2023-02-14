# CoreEx.Invokers

The `CoreEx.Invokers` namespace provides invocation capabilities.

<br/>

## Motivation

To enable a standardized approach to the invocation of logic enabling a means to add surrounding runtime execution logic decoupled from the initial implementation; enabling additional functionality (i.e. logging) to be added separately where desired.

<br/>

## Base capabilities

The [`InvokerBase<TInvoker>`](./InvokerBaseT.cs) and [`InvokerBase<TInvoker, TArgs>`](./InvokerBaseT2.cs) provide the base functionality to invoke an underlying `Action` or `Func` either synchronously or asynchronously. The virtual `OnInvokeAsync` method must be overridden to extend the functionality.

<br/>

### Usage

The general usage pattern is to provide a concrete implementation, for example [`DatabaseInvoker`](../../CoreEx.Database/DatabaseInvoker.cs) and then leverage within application to invoke (wrap) key logic. To then enable runtime overridding, the owning class would allow an instance to be provided with the constructor (i.e. Dependency Injection), for example [`Database`](../../CoreEx.Database/Database.cs).

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


