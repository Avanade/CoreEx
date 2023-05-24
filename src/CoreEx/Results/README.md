# CoreEx.Results

The `CoreEx.Result` namespace enables [monadic](https://en.wikipedia.org/wiki/Monad_(functional_programming)) error-handling, often referred to [Railway-oriented programming](https://swlaschin.gitbooks.io/fsharpforfunandprofit/content/posts/recipe-part2.html), as illustrated below (from referenced article) - this is a **must** read for context and understanding of the benefits of this approach.

![Railway-oriented](https://swlaschin.gitbooks.io/fsharpforfunandprofit/content/assets/img/Recipe_Railway_Transparent.png)

<br/>

## Motivation

To provide a means to enable Railway-oriented programming where leveraging _CoreEx_, and within own development, in a rich and consistent manner. The capabilities are for the most part completely optional and can be used as needed. Although, C# is not a functional language, it does support a number of functional concepts (i.e. LINQ), and adding this capability to _CoreEx_ is in keeping withing the spirit of railway-orientation.

<br/>

### Cost of exceptions

There are some logic and performance benefits for leveraging, especially where managing errors, as this can avoid the traditional throwing of exceptions, which can be expensive, and instead provides a means to manage and return errors in a more functional manner.

There are a number of articles on the internet, that provide concrete examples of the performance challenges that can come with the usage of exceptions, that clearly demonstrate the potential impact.

This is not to say the exceptions are bad and should be avoided, they absolutely serve a purpose and should continue to be leveraged in those truly _exceptional_ cases. Microsoft provides [guidance](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions-and-performance) around this specifically; also review their [best practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions) for exceptions.

<br/>

### Exception vs error

However, in some instances where returning more of a business/logic error (versus an unexpected exception) that is intended to be handled by the consumer then an exception, although convenient, is possibly not the best approach.

For example, where exposing an API that supports the updating of an entity, and the entity data is not valid, then more typically some sort of validation exception would be thrown by the business logic, then re-caught by the API logic and finally transformed in a corresponding HTTP 400 error. This validation error is intended and expected behaviour, i.e. not _exceptional_, and therfore treating it as an explicit error is more appropriate.

However, in the example, where persisting the entity to a database, and the database is unavailable, then this is an _exceptional_ case, and therefore throwing an exception as areult is more appropriate.

Finally, an exception contains other context, such as the stack trace to assist with the likes of troubleshooting, which is generally not required for an explicit (expected) business error.

<br/>

## Results

_CoreEx_ enables using the following types, which can be used to represent either a successful result or an error result.

Class | Description
-|-
[`Result`](./Result.cs) | Represents the outcome of an operation with _no_ value. It is intended to be a replacement for `void`-based methods, where the `void` is replaced with a `Result` that can be used to represent the outcome of the operation. The static `Result.Success` property represents the successful `Result` instance.
[`Result<T>`](./ResultT.cs) | Represents the outcome of an operation _with_ a `Value` (of type `T`). It is intended to be a used where a method previously returns a value. The static `Result<T>.None` property represents the successful `Result<T>` instance with its [default](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/default) value.

They results each additionally contain the following key properties as per [`IResult`](./IResult.cs).

Property | Description
-|-
`IsSuccess` | Indicates whether the operation was successful.
`IsFailure` | Indicates whether the operation was a failure.
`Error` | The failure error, represented as an `Exception`. The .NET [`Exception`](https://learn.microsoft.com/en-us/dotnet/api/system.exception) is used for the error type as it already provides a rich set of capabilities, can easily be thrown where applicable, has support for the likes of an [`AggregateException`](https://learn.microsoft.com/en-us/dotnet/api/system.aggregateexception) for combining, and is well understood by developers; i.e. there is limited benefit in creating a new custom error type.

<br/>

### Success

By default, each of the results has a default property to represent success,`Result.Success` and `Result<T>.None` (default value). 

Additionally, the `Result<T>.Ok(T value)` method enables the creation of a successful result with the specified value. The `Result<T>` also supports [implicit](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators) conversion from `T` to `Result<T>` and vice versa.

<br/>

### Failures

The following primary failure methods are provided.

Method | Description
-|-
`Fail` | Creates a failure result with the specified error message (internally creates a [`BusinessException`](../BusinessException.cs)), or alternatively can be passed an `Exception` directly.
`ThrowOnError` | Throws the `Error` if the result is a failure; otherwise, does nothing.

The `Result` and `Result<T>` also support [implicit](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators) conversion from an `Exception`.

The following secondary failure methods are provided that enable the creation of a failure result with a specific _CoreEx_ error type; these exceptions are used extensively within _CoreEx_ to both represent specifixc error types and to enable related functionality as a result of the exception typically being thrown.

Method | Description
-|-
`AuthenticationError` | Creates a failure result with the [`AuthenticationException`](../AuthenticationException.cs).
`AuthorizationError` | Creates a failure result with the [`AuthorizationException`](../AuthorizationException.cs).
`ConcurrencyError` | Creates a failure result with the [`ConcurrencyException`](../ConcurrencyException.cs).
`ConflictError` | Creates a failure result with the [`ConflictException`](../ConflictException.cs).
`DuplicateError` | Creates a failure result with the [`DuplicateException`](../DuplicateException.cs).
`NotFoundError` | Creates a failure result with the [`NotFoundException`](../NotFoundException.cs).
`TransientError` | Creates a failure result with the [`TransientException`](../TransientException.cs).
`ValidationError` | Creates a failure result with the [`ValidationException`](../ValidationException.cs).

All of the above methods support the passing of the error message, which leverages [`LText`](../Localization/LText.cs) to enable the localization of the error message.

<br/>

### Binding (converting)

It is a common requirement where leveraging results that the result type needs to be converted to another type, either from/to a `Result` and `Result<T>`, or between different `Result<T>` types. The [`Result.Bind`](./CoreExtensions.cs) extension methods and its various overloads enable. 

Where converting from a `Result<T>` to a `Result` the `Value` will be lost, and where converting from a `Result` to a `Result<T>` the `Result<T>.None` value is used. 

Additionally, where converting between different `Result<T>` types the `Value` will be converted from one type to another using a [`TypeConverter`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter) where possible; otherwise, the `Value` will be lost.

Finally, there is implicit conversion support between `Result` and `Result<T>` as there is no data loss; and explict conversion (casting) support between `Result<T>` and `Result` given the data loss (this is intended to minimize unintentional data loss).

<br/>

### Combining

The [`Result.Combine`](./CoreExtensions.cs) extension methods and its various overloads enable combining two results into a single result. Where combining and there are multiple failures, then these will be combined into a single `Error`leveraging an underlying [`AggregateException`](https://learn.microsoft.com/en-us/dotnet/api/system.aggregateexception).

<br/>

## Composition

To leverage the results beyond the basics of a response mechanism, the composition of multiple steps brings the real power of railway-oriented programming. This is implemented within .NET leveraging a fluent-style method-chaining approach.

The following extension methods are provided to enable the composition of results; these are success and failure aware, as well as supporting binding (conversion) between different result types.

Method | Description
-|-
[`Go`](./ResultGo.cs), `GoAsync` | Begins (starts) a new result chain.
[`Then`](./ThenExtensions.cs), `ThenAsync`, `ThenAs`, `ThenAsAsync` | Executes the specified function if the result is a success; otherwise, does nothing.
[`When`](./WhenExtensions.cs), `WhenAsync`, `WhenAs`, `WhenAsAsync` | Executes the specified function if the result is a success and the corresponding condition evaluates to _true_; otherwise, does nothing.
[`OnFailure`](./OnFailureExtensions.cs), `OnFailureAsync`, `OnFailureAs`, `OnFailureAsAsync`  | Executes the specified function if the result is a failure; otherwise, does nothing.
[`Match`](./MatchExtensions.cs), `MatchAsync`, `MatchAs`, `MatchAsAsync` | Executes (matches) the _ok_ function when the result is a success; otherwise, invokes the corresponding _fail_ function.
[`Any`](./AnyExtensions.cs), `AnyAsync`, `AnyAs`, `AnyAsAsync` | Executes the specified function regardless of the result state.

The methods above that are named with `As` are to support _explicit_ conversion between types to minimize unintentional data loss and/or unexpected side-effects. The methods above that are named with `Async` are to support asynchronous execution.

For the most part the above also support the following interfaces, which can be applied to other types to enable the composition of results: [`IToResult`](./IToResult.cs), [`IToResult<T>`](./IToResultT.cs) and [`ITypedToResult`](./ITypedToResult.cs). For example, the _CoreEx_ [`IValidationResult`](../Validation/IValidationResult.cs) implements the `ITypedToResult` to enable the composition of validation results.

<br/>

### Simple example

The following [`DatabaseQuery`](../../CoreEx.Database/Extended/DatabaseQuery.cs) code demonstrates usage; with the key takeaway being that each step in the chain is only executed if the previous step was a success.

``` csharp
private async Task<Result<TResult>> SelectWrapperWithResultAsync<TResult>(Func<DatabaseCommand, CancellationToken, Task<Result<TResult>>> func, CancellationToken cancellationToken)
{
    var rvp = Paging != null && Paging.IsGetCount ? Parameters.AddReturnValueParameter() : null;
    var cmd = Command.Params(Parameters).PagingParams(Paging);

    return await Result.GoAsync(func(cmd, cancellationToken))
                       .When(_ => rvp != null && rvp.Value != null, _ => { Paging!.TotalCount = (long)rvp!.Value; })
                       .Then(res => QueryArgs.CleanUpResult ? Cleaner.Clean(res) : res);
}
```

<br/>

### Complex example

The following [`CosmosDbValueContainer`](../../CoreEx.Cosmos/CosmosDbValueContainer.cs) code demonstrates usage; with the key takeaway being steps can contain complex logic, the type can be explicitly converted by the use of the `ThenAsAsyc()`, and errors can be returned where applicable as evidenced by the usage of the `Result.ConcurrencyError()`.

``` csharp
return await Result
    .Go(CheckAuthorized(resp))
    .When(() => v is IETag etag2 && etag2.ETag != null && ETagGenerator.FormatETag(etag2.ETag) != resp.ETag, () => Result.ConcurrencyError())
    .Then(() =>
    {
        ro.SessionToken = resp.Headers?.Session;
        ChangeLog.PrepareUpdated(v);
        CosmosDb.Mapper.Map(v, resp.Resource, OperationTypes.Update);
        Cleaner.ResetTenantId(resp.Resource);

        // Re-check auth to make sure not updating to something not allowed.
        return CheckAuthorized(resp);
    })
    .ThenAsAsync(async () =>
    {
        resp = await Container.ReplaceItemAsync(resp.Resource, key, pk, ro, ct).ConfigureAwait(false);
        return GetResponseValue(resp)!;
    });
```