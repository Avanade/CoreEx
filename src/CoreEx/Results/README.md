# CoreEx.Results

The `CoreEx.Result` namespace enables [monadic](https://en.wikipedia.org/wiki/Monad_(functional_programming)) error-handling, often referred to as [Railway-oriented programming](https://swlaschin.gitbooks.io/fsharpforfunandprofit/content/posts/recipe-part2.html), as illustrated below (from referenced article) - this is a **must** read for context and understanding of the benefits of this approach.

![Railway-oriented](../../../images/Railway_Transparent.png)

<br/>

## Motivation

To provide a means to enable Railway-oriented programming within _CoreEx_, and where leveraging, in a rich and consistent manner. The capabilities are for the most part completely optional and can be used as needed. Although C# is not a functional language, it does support a number of functional concepts (i.e. LINQ), and adding this capability to _CoreEx_ is in keeping within the spirit of this.

<br/>

### Cost of exceptions

There are some logic and performance benefits for leveraging, especially where managing errors, as this can avoid the traditional throwing of exceptions; and instead, provides a means to manage and return errors in a more functional manner.

There are a number of [articles](https://csharpplayersguide.com/blog/2022/03/12/exception-performance/) on the internet that provide concrete examples of the performance challenges that can come with the usage of exceptions, these clearly demonstrate the potential impact.

This is not to say that exceptions are bad and should be avoided, they absolutely serve a purpose and should continue to be leveraged in those truly _exceptional_ cases. Microsoft provides [guidance](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions-and-performance) around this specifically; also review their [best practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions) for exceptions.

<br/>

### Exception vs error

However, in some instances where returning a business functional error that is intended to be handled by the consumer then an exception, although convenient, is possibly not the best approach.

For example, where exposing an API that supports the updating of an entity, and the entity data is not valid, then more typically some sort of validation exception would be thrown by the business logic, then re-caught by the API logic, and finally transformed in a corresponding HTTP 400 error. This validation error is intended and expected behaviour, i.e. it is _not_ exceptional, and therefore treating it as an explicit _error_ is the most appropriate course of action.

However, in this example, where persisting the entity to a database, and the database is unavailable, then this is an unexpected _exceptional_ case, and throwing a corresponding exception as a result is appropriate.

Finally, an exception contains additional context, such as the stack trace to assist with the likes of troubleshooting, which is generally not required for an explicit (expected) business error.

<br/>

## Results

_CoreEx_ enables using the following two types, which can be used to represent either a successful result or an error result.

Class | Description
-|-
[`Result`](./Result.cs) | Represents the outcome of an operation with _no_ value. It is intended to be a replacement for `void`-based methods, where the `void` is replaced with a `Result` that can be used to represent the outcome of the operation. The static `Result.Success` property represents the successful `Result` instance.
[`Result<T>`](./ResultT.cs) | Represents the outcome of an operation _with_ a `Value` (of type `T`). It is intended to be a used where a method previously returns a value. The static `Result<T>.None` property represents the successful `Result<T>` instance with its [default](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/default) value.

The results each additionally contain the following key properties as per [`IResult`](./IResult.cs).

Property | Description
-|-
`IsSuccess` | Indicates whether the operation was successful.
`IsFailure` | Indicates whether the operation was a failure.
`Error` | The failure error, represented as an `Exception`. The .NET [`Exception`](https://learn.microsoft.com/en-us/dotnet/api/system.exception) is used for the error type as it already provides a rich set of capabilities, can easily be thrown where applicable, has support for the likes of an [`AggregateException`](https://learn.microsoft.com/en-us/dotnet/api/system.aggregateexception) for combining, and is well understood by developers; i.e. it was determined that there is limited benefit in creating an alternate error type.

<br/>

### Success

By default, each of the results has a default property to represent success, `Result.Success` and `Result<T>.None` (default value). 

Additionally, the `Result<T>.Ok(T value)` method enables the creation of a successful result with the specified value. The `Result<T>` also supports [implicit](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators) conversion from `T` to `Result<T>` and vice-versa.

<br/>

### Failures

The following primary failure methods are provided.

Method | Description
-|-
`Fail()` | Creates a failure result with the specified error message (internally creates a [`BusinessException`](../BusinessException.cs)), or alternatively can be passed an `Exception` directly.
`ThrowOnError()` | Throws the `Error` if the result is a failure; otherwise, does nothing.

The `Result` and `Result<T>` also support [implicit](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators) conversion from an `Exception`.

The following secondary failure methods are provided that enable the creation of a failure result with a specific _CoreEx_ error type. These [exceptions](../README.md/#exceptions) are used extensively within _CoreEx_ to both represent specific error types, and to enable related functionality as a result of the exception typically being thrown.

Method | Description
-|-
`AuthenticationError()` | Creates a failure result with the [`AuthenticationException`](../AuthenticationException.cs).
`AuthorizationError()` | Creates a failure result with the [`AuthorizationException`](../AuthorizationException.cs).
`ConcurrencyError()` | Creates a failure result with the [`ConcurrencyException`](../ConcurrencyException.cs).
`ConflictError()` | Creates a failure result with the [`ConflictException`](../ConflictException.cs).
`DuplicateError()` | Creates a failure result with the [`DuplicateException`](../DuplicateException.cs).
`NotFoundError()` | Creates a failure result with the [`NotFoundException`](../NotFoundException.cs).
`TransientError()` | Creates a failure result with the [`TransientException`](../TransientException.cs).
`ValidationError()` | Creates a failure result with the [`ValidationException`](../ValidationException.cs).

All of the above methods support the passing of the error message, which leverages [`LText`](../Localization/LText.cs) to enable the localization of the error message.

<br/>

### Binding (converting)

It is a common requirement where leveraging results that the result type needs to be converted to another type, either from/to a `Result` and `Result<T>`, or between different `Result<T>` types. The [`Result.Bind`](./CoreExtensions.cs) extension methods and its various overloads enable. 

Where converting from a `Result<T>` to a `Result` the `Value` will be lost, and where converting from a `Result` to a `Result<T>` the `Result<T>.None` value is used. 

Additionally, there is implicit conversion support from `Result` to `Result<T>` as there is no data loss; and explict conversion (casting) support from `Result<T>` to `Result` given the data loss (this is intended to minimize unintentional data loss).

Finally, as a general rule the binding will be performed automatically by the framework (i.e. is used primarily internally).

<br/>

### Combining

The [`Result.Combine`](./CoreExtensions.cs) extension methods and its various overloads enable combining two results into a single result. Where combining and there are multiple failures, then these will be combined into a single `Error` leveraging an underlying [`AggregateException`](https://learn.microsoft.com/en-us/dotnet/api/system.aggregateexception).

<br/>

## Composition

To leverage the results beyond the basics of a response mechanism, the composition of multiple steps brings the real power of railway-oriented programming. This is implemented within .NET leveraging a fluent-style method-chaining approach.

The following extension methods are provided to enable the composition of results; these are success and failure aware, as well as supporting binding (conversion) between different result types.

Method | Description
-|-
[`Go()`](./ResultGo.cs), `GoAsync()`, `GoFrom()`, `GoFromAsync()` | Begins (starts) a new result chain.
[`Then()`](./ThenExtensions.cs), `ThenAsync()`, `ThenAs()`, `ThenAsAsync()`, `ThenFrom()`, `ThenFromAsync()`, `ThenFromAs()`, `ThenFromAsAsync()` | Executes the specified function if the result is a success; otherwise, does nothing.
[`When()`](./WhenExtensions.cs), `WhenAsync`, `WhenAs()`, `WhenAsAsync()`, `WhenFrom()`, `WhenFromAsync()`, `WhenFromAs()`, `WhenFromAsAsync()` | Executes the specified function if the result is a success and the corresponding condition evaluates to _true_; otherwise, does nothing.
[`OnFailure()`](./OnFailureExtensions.cs), `OnFailureAsync()`, `OnFailureAs()`, `OnFailureAsAsync()`  | Executes the specified function if the result is a failure; otherwise, does nothing.
[`Match()`](./MatchExtensions.cs), `MatchAsync()`, `MatchAs()`, `MatchAsAsync()` | Executes (matches) the _ok_ function when the result is a success; otherwise, invokes the corresponding _fail_ function.
[`Any()`](./AnyExtensions.cs), `AnyAsync()`, `AnyAs()`, `AnyAsAsync()` | Executes the specified function regardless of the result state.
[`AsResult()`](./ResultsExtensions.cs), `AsResultAsync()` | Converts (binds) the `Result<T>` to a `Result` (i.e. loses the `Value`); or a `Result<T>` to a `Result<U>`.

By convention methods that are named with the following have the following characteristics.

Convention | Description
-|-
`As` | Supports _explicit_ conversion between types to minimize unintentional data loss and/or unexpected side-effects.
`Async` | Supports asynchronous execution (versus synchronous).
`From` | Supports [`IToResult`](./IToResult.cs), [`IToResult<T>`](./IToResultT.cs) and [`ITypedToResult`](./ITypedToResult.cs) result conversion.

<br/>

### ToResult

The [`IToResult`](./IToResult.cs), [`IToResult<T>`](./IToResultT.cs) and [`ITypedToResult`](./ITypedToResult.cs) interfaces enable the conversion of a type to a result. These have been added to the following types.

Namespace | Type(s)
-|-
[`CoreEx.Http`](../Http) | [`HttpResult`](../Http/HttpResult.cs) implements `IToResult`. <br/> [`HttpResult<T>`](../Http/HttpResultT.cs) implements `IToResult<T>`.
[`CoreEx.Validation`](../Validation) | [`IValidationResult`](../Validation/IValidationResult.cs) implements `ITypedToResult`.

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

<br/>

### ToResult example

The following code demonstrates the usage of the [`IToResult<T>`](./IToResult<T>.cs) interface enabled by the `HttpResult<T>`; with the key takeaway being that the `Result.GoFrom()` method is used.

``` csharp
public async Task<Result<OktaUser>> GetUserAsync(Guid id, string email) 
    => Result.GoFrom(await GetAsync<List<OktaUser>>($"/api/v1/users?search=profile.email eq \"{email}\"").ConfigureAwait(false))
        .ThenAs(coll => coll.Count switch 
        {
            0 => Result.NotFoundError($"Employee {id} with email {email} not found within OKTA."),
            1 => Result.Ok(coll[0]),
            _ => Result.NotFoundError($"Employee {id} with email {email} has multiple entries within OKTA.")
        });
```

<br/>

### Additional

The following additional composition extensions methods are available:

Method | Description
-|-
`ValidateAsync` | Validates the `Result<T>.Value` using either the specified [`IValidator<T>`](../Validation/IValidatorT.cs) or [`IPropertyRule<ValidationValue<TEntity?>, TEntity?>`](../../CoreEx.Validation/IPropertyRuleT2.cs) resulting in either success or failure (`Result<T>.ValidationError`). Include `CoreEx.Validation` namespace to enable.
`ValidatesAsync` | Validates the specified _value_ using either the specified [`IValidator<T>`](../Validation/IValidatorT.cs) or [`IPropertyRule<ValidationValue<TEntity?>, TEntity?>`](../../CoreEx.Validation/IPropertyRuleT2.cs) resulting in either success or failure (`Result<T>.ValidationError`). Include `CoreEx.Validation` namespace to enable.
`Required` | Validates that the `Result<T>.Value` is non-default (i.e. is required) resulting in either success or failure (`Result<T>.ValidationError`). Include `CoreEx.Validation` namespace to enable.
`Requires` | Validates that the specified _value_ is non-default (i.e. is required) resulting in either success or failure (`Result<T>.ValidationError`). Include `CoreEx.Validation` namespace to enable.
`ThrowIfNull` | Throws a [`NullReferenceException`](https://docs.microsoft.com/en-us/dotnet/api/system.nullreferenceexception) if the `Result<T>.Value` is `null`. 
`CacheGetOrAddAsync` | Gets the [`IRequestCache`](../Caching/IRequestCache.cs) cached value associated with the specified key where it exists; otherwise, adds and returns the value created by the corresponding add factory function. Include `CoreEx.Caching` namespace to enable.
`UserIsAuthorized` | Performs the equivalent `ExecutionContext.Current.UserIsAuthorized` resulting in either success or failure.
`UserIsInRole` | Performs the equivalent `ExecutionContext.Current.UserIsInRole` resulting in either success or failure.

<br/>

## WithResult

Within _CoreEx_ `Result` and `Result<T>` support has been enabled; these are methods generally suffixed by `WithResult` or `WithResultAsync` by convention. These co-exist with the existing exception throwing methods. 

This allows these methods to be conveniently invoked without the need to explicitly catch the related exceptions as the `IResult.Error` will have been set accordingly. Note that only exceptions that correspond to the _CoreEx_ [secondary failures](#Failures) will be caught and converted; otherwise, the _exceptional_ exceptions will continue to be thrown. These would then need to be explicitly caught and handled where applicable.

The `*WithResult` or `*WithResultAsync` methods have been added to the following types:

Namespace | Type(s)
-|-
[`CoreEx.Cosmos`](../../CoreEx.Cosmos) | [`CosmosDbContainerBase`](../../CoreEx.Cosmos/CosmosDbContainerBase.cs), [`CosmosDbContainer`](../../CoreEx.Cosmos/CosmosDbContainer.cs), [`CosmosDbValueContainer`](../../CoreEx.Cosmos/CosmosDbValueContainer.cs), [`CosmosDbQueryBase`](../../CoreEx.Cosmos/CosmosDbQueryBase.cs), [`CosmosDbQuery`](../../CoreEx.Cosmos/CosmosDbQuery.cs) and [`CosmosDbValueQuery`](../../CoreEx.Cosmos/CosmosDbValueQuery.cs).
[`CoreEx.Database`](../../CoreEx.Database) | [`DatabaseCommand`](../../CoreEx.Database/DatabaseCommand.cs), [`DatabaseQuery`](../../CoreEx.Database/Extended/DatabaseQuery.cs), [`RefDataLoadeder`](../../CoreEx.Database/Extended/RefDataLoader.cs) and [`DatabaseExtendedExtensions`](../../CoreEx.Database/Extended/DatabaseExtendedExtensions.cs).
[`CoreEx.EntityFrameworkCore`](../../CoreEx.EntityFrameworkCore) | [`IEfDb`](../../CoreEx.EntityFrameworkCore/IEfDb.cs), [`EfDb`](../../CoreEx.EntityFrameworkCore/EfDb.cs) and [`EfDbEntity`](../../CoreEx.EntityFrameworkCore/EfDbEntity.cs).
[`CoreEx.WebApis`](../WebApis) | [`WebApiBase`](../WebApis/WebApiBase.cs), [`WebApi`](../WebApis/WebApi.cs) and [`WebApiPublisher`](../WebApis/WebApiPublisher.cs).
