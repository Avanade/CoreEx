# CoreEx.Http

The `CoreEx.Http` namespace provides additional HTTP capabilities.

<br/>

## Motivation

To encapsulate and enrich the `HttpClient` experience simplifying advanced scenarios, such as retries for the likes of transient errors, and supporting timeouts, etc.

<br/>

## Typed HttpClient

Provides capabilities to enable extended [typed](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests#typed-clients) [`HttpClient`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) functionality providing a fluent-style method-chaining to enable the likes of `WithRetry`, `EnsureSuccess`, `Timeout`, and `ThrowTransientException`, etc. to improve the per invocation experience.

Class | Description
-|-
[`TypedHttpClientBase`](./TypedHttpClientBase.cs) | Provides the base foundational abstract capabilities.
[`TypedHttpClientBase<TSelf>`](./TypedHttpClientBaseT.cs) | Extends `TypedHttpClientBase` adding the abstract fluent-style method-chaining capabilities and supporting `SendAsync` logic.
[`TypedHttpClientCore<TSelf>`](./TypedHttpClientCore.cs) | Extends `TypedHttpClientBase<TSelf>` adding abstract support for `Head`, `Get`, `Post`, `Put`, `Patch` and `Delete` HTTP methods.
[`TypedHttpClient`](./TypedHttpClient.cs) | Provides `TypedHttpClientCore<TSelf>` implementation encapsulating an `HttpClient`.

<br/>

### Options

The [`TypedHttpClientOptions`](./Extended/TypedHttpClientOptions.cs) houses the fluent-style method-chaining options for a [`TypedHttpClientBase<TSelf>`](./TypedHttpClientBaseT.cs) via an underlying `DefaultOptions` property. This enables the standardized configuration that will be used for each request, versus configuring per-request directly. Once a request has completed the `TypedHttpClientBase<TSelf>` will reset to the `DefaultOptions`.

<br/>

### Fluent-style method-chaining

The fluent-style method-chaining capabilities are as follows.

Method | Description
-|-
`Ensure` | Adds the [`HttpStatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode)(s) to the accepted list to be verified against the resulting [`StatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.statuscode).
`EnsureAccepted` | Adds the [`HttpStatusCode.Accepted`](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-accepted) to the accepted list to be verified against the resulting [`StatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.statuscode).
`EnsureCreated` | Adds the [`HttpStatusCode.Created`](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-created) to the accepted list to be verified against the resulting [`StatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.statuscode).
`EnsureNoContent` | Adds the [`HttpStatusCode.NoContent`](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-nocontent) to the accepted list to be verified against the resulting [`StatusCode`](https://learn.microsoft.com/noconetnten-us/dotnet/api/system.net.http.httpresponsemessage.statuscode).
`EnsureNotFound` | Adds the [`HttpStatusCode.NotFound`](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-notfound) to the accepted list to be verified against the resulting [`StatusCode`](https://learn.microsoft.com/noconetnten-us/dotnet/api/system.net.http.httpresponsemessage.statuscode).
`EnsureOK` | Adds the [`HttpStatusCode.OK`](https://learn.microsoft.com/en-us/dotnet/api/system.net.httpstatuscode#system-net-httpstatuscode-ok) to the accepted list to be verified against the resulting [`StatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.statuscode).
`EnsureSuccess` | Specifies whether to automatically perform a [`HttpResponseMessage.EnsureSuccessStatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.ensuresuccessstatuscode).
`NullOnNotFound` | Specifies that a `null`/`default` value is returned where the _response_ has a `HttpStatusCode.NotFound` (applicable to an HTTP `GET` only).
`OnBeforeRequest` | Specifies the [function](https://learn.microsoft.com/en-us/dotnet/api/system.func-3) to update the [`HttpRequestMessage`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage) before the request is sent. 
`ThrowKnownException` | Specifies that the [`IExtendedException`](../Abstractions/IExtendedException.cs) exception implementation for the HTTP status code is thrown when encountered.
`ThrowTransientException` | Specifies that a [`TransientException`](../TransientException.cs) is thrown when a transient error is encountered.
`WithMaxRetryDelay` | Specifies the max retry delay that polly retries will be capped with.
`WithCustomRetryPolicy` | Specifies a custom retry policy; overridding the default.
`WithRetry` | Specifies a retry, including count and delay seconds (exponential), where a transient error is encountered.
`WithTimeout` | Specifies the timeout for a request.

<br/>

### Request options

The [`HttpRequestOptions`](./HttpRequestOptions.cs) enable additional standardized options to be specified per request where applicable.

<br/>

### Result

The [`HttpResult`](./HttpResult.cs) and [`HttpResult<T>`](./HttpResultT.cs) provide a standardized result that encapsulates the underlying [`HttpResponseMessage`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage); including the underlying JSON deserialization of underlying value.

<br/>

### Example

The following demonstrates usage.

``` csharp
public class XxxAgent : TypedHttpClientCore<XxxAgent>
{
    public XxxwAgent(HttpClient client, IJsonSerializer jsonSerializer, CoreEx.ExecutionContext executionContext, SettingsBase settings, ILogger<XxxAgent> logger)
        : base(client, jsonSerializer, executionContext, settings, logger)
    {
        DefaultOptions.WithRetry();
    }
}

...

var hr = await _xxxAgent.EnsureOK().EnsureNotFound().PostAsync<dynamic, int>("foo/bar", new { trackerId = id }).ConfigureAwait(false);
if (hr.StatusCode == HttpStatusCode.NotFound)
    return -1;

return hr.Value;
```

<br/>

## Extended

The [`CoreEx.Http.Extended`](./Extended) namespace enables extended Typed HttpClient types where request and response [mapping](../Mapping) is also included.
