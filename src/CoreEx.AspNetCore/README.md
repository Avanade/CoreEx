# CoreEx.AspNetCore

The `CoreEx.AspNetCore` namespace provides extended capabilities to build Web APIs, for the likes of [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet/apis) or [HTTP-triggered Azure functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger). The [`WebApi`](./WebApis/WebApi.cs) and [`WebApiPublisher`](./WebApis/WebApiPublisher.cs) capabilities (within the `CoreEx.AspNetCore.WebApis` namespace)  encapsulate the consistent handling of the HTTP request and corresponding response, whilst also providing additional capabilities that are not available out-of-the-box within the .NET runtime.

<br/>

## Motivation

To standardize, and simplify, the development of JSON-based Web APIs. The key integration patterns currently being addressed are as follows:

Pattern | Description | Capability
-|-|-
Request-response | This represents a real-time request-response, whereby the request is immediately fulfilled (synchronous) with the response representing the result of the request. | [WebApi](#webapi)
Fire-and-forget | This is to enable decoupled asynchronous processing, whereby the request is immediately accepted (queued internally), with a separate internal process that fulfils the request independently of the request. | [WebApiPublish](#webapipublish)

<br/>

## Limitations

Only JSON-based Web APIs are generally supported. Where additional or other content types are needed then this library in its current state will not be able to enable, and these Web APIs will need to be implemented in a traditional custom manner.

There is provision such that any result of type [`IActionResult`](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types), for example [`FileContentResult`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.filecontentresult), is returned these will be enacted by the ASP.NET Core runtime as-is (i.e. no `CoreEx.AspNetCore` processing will occur on the result). However, all other request handling, exception handling, logging, etc. described below will occur which has a consistency benefit.

<br/>

## WebApi

The [`WebApi`](./WebApis/WebApi.cs) class should be leveraged as the primary means to enable Web API functionality, it provides methods for HTTP `GET`, `POST`, `PUT`, `PATCH` and `DELETE` operations that encapsulates the execution in a standardized manner, providing alternate overloads and options to enable the desired behaviours.

The `WebApi` extends (inherits) [`WebApiBase`](./WebApis/WebApiBase.cs) that provides the base `RunAsync` method that all other methods invoke to wrap the underlying logic. This in turns invokes the [`WebApiInvoker`](./WebApis/WebApiInvoker.cs) which provides a pluggable mechanism (i.e. can be replaced) that by default handles the following consistently for each request:

- Infers the standard [`WebApiRequestOptions`](./WebApis/WebApiRequestOptions.cs) from the HTTP request headers and query string (names are configurable).
- Infers the correlation identifier from the HTTP request header (names are configurable).
- Begins a logging scope to include the correlation identifier.
- Invokes the request logic and returns the corresponding [`IActionResult`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.iactionresult).
- Handle all exceptions:
  - Where the exception implements [`IExtendedException`](../CoreEx/Abstractions/IExtendedException.cs) then returns `IExtendedException.ToResult()`. Also, where `IExtendedException.ShouldBeLogged` is `true` then a `ILogger.LogError` will occur; some errors, such as `400-BadRequest`, need not be logged as they are not a run-time error per se.
  - Invoke the protected `OnUnhandledExceptionAsync` then return resulting `IActionResult` where not `null`.

<br/>

### Supported HTTP methods

`WebApi` provides the following per HTTP method; each with varying overloads depending on need. Where a generic `Type` is specified, either `TValue` being the request content body and/or `TResult` being the response body, this signifies that `WebApi` will manage the underlying JSON serialization:

HTTP | Method | Description
-|-|-
`GET` | `GetAsync<TResult>()` | Performs a `GET` operation. 
`POST` | `PostAsync()` <br/> `PostAsync<TValue>()` <br/> `PostAsync<TResult>()` <br/> `Post<TValue, TResult>()` | Performs a `POST` operation.
`PUT` | `PutAsync<TValue>()` <br/> `PutAsync<TValue, TResult>()` | Performs a `PUT` operation.
`PATCH` | `PatchAsync<TValue>` | Performs a `PATCH` operation. Support for [`application/merge-patch+json`](https://tools.ietf.org/html/rfc7396) with [`JsonMergePatch`](../CoreEx/Json/Merge/JsonMergePatch.cs).
`DELETE` | `DeleteAsync()` | Performs a `DELETE` operation.
`*` | `RunAsync()` <br/> `RunAsync<TValue>()` | Performs _any_ operation returning an `IActionResult`.

<br/>

### Request

Where a request contains a content body that contains JSON (content-type of `application/json`) then these methods _can_ (where the `TValue` is defined) perform the deserialization using the appropriate [`IJsonSerailizer`](../CoreEx/Json/IJsonSerializer.cs). The corresponding [`WebApiRequestOptions`](./WebApis/WebApiRequestOptions.cs) are also automatically inferred as described above.

Where using `CoreEx` to perform the JSON deserialization then the value is _not_ specified as an argument within the method (typically with the [`FromBody`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.frombodyattribute) attribute). However, this will mean that the value type is not output when _Swagger_ output is generated; to enable, use the [`AcceptsBody`](./WebApis/AcceptsBodyAttribute.cs) attribute to specify. 

<br/>

### Response
 
Where a `TResult` value is returned then these methods will perform the JSON serialization, using the appropriate `IJsonSerailizer`. This is managed by the underlying [`ValueContentResult.CreateResult`](./WebApis/ValueContentResult.cs) which additionally performs the following:

Step | Description
-|-
[`PagingResult`](../CoreEx/Entities/PagingResult.cs) headers | Where response value is [`ICollectionResult`](..//CoreEx/Entities/ICollectionResult.cs) then sets `PagingResult` headers and returns underlying collection (`ICollectionResult.Collection`).
JSON serialization | Serializes the `TResult` value using the `IJsonSerailizer`. Where include or exclude fields were specified within the request query string then these will be applied (`IJsonSerializer.TryApplyFilter`) to the JSON response to limit the response content.
`ETag` generation | Checks if value implements [`IETag`](../CoreEx/Entities/IETag.cs), where non-null leave as-is; otherwise, automatically [generate](../CoreEx/Abstractions/ETagGenerator.cs) `ETag` hash from serialized value (excluding filters).
`GET` [`If-Match`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-Match) | Where the value/generated `ETag` equals the `GET` request `If-Match` value then return an HTTP status code of `304-NotModified` with no content.
[`ETag`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag) header | Sets the HTTP `ETag` header using either `IETag.ETag` or generated hash.
[Status code](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status) | Sets the response HTTP status code as configured.
[`Location`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Location) | Sets the HTTP `Location` header where specified (where applicable).

As described earlier, the above will _not_ occur for `IActionResult` results.

<br/>

### ASP.NET example

The following demonstrates usage when creating an ASP.NET `Controller`:

``` csharp
[Route("api/employees")]
public class EmployeeController : ControllerBase
{
    private readonly WebApi _webApi;
    private readonly EmployeeService _service;

    public EmployeeController(WebApi webApi, EmployeeService service)
    {
        _webApi = webApi;
        _service = service;
    }

    [HttpGet("{id}", Name = "Get")]
    public Task<IActionResult> GetAsync(Guid id)
        => _webApi.GetAsync(Request, _ => _service.GetEmployeeAsync(id));

    [HttpGet("", Name = "GetAll")]
    public Task<IActionResult> GetAllAsync()
        => _webApi.GetAsync(Request, p => _service.GetAllAsync(p.RequestOptions.Paging));

    [HttpPost("", Name = "Create")]
    public Task<IActionResult> CreateAsync()
        => _webApi.PostAsync<Employee, Employee>(Request, p => _service.AddEmployeeAsync(p.Validate<Employee, EmployeeValidator>()),
           statusCode: HttpStatusCode.Created, locationUri: e => new Uri($"api/employees/{e.Id}", UriKind.RelativeOrAbsolute));

    [HttpPut("{id}", Name = "Update")]
    public Task<IActionResult> UpdateAsync(Guid id)
        => _webApi.PutAsync<Employee, Employee>(Request, p => _service.UpdateEmployeeAsync(p.Validate<Employee, EmployeeValidator>(), id));

    [HttpPatch("{id}", Name = "Patch")]
    public Task<IActionResult> PatchAsync(Guid id)
        => _webApi.PatchAsync(Request, get: _ => _service.GetEmployeeAsync(id), put: p => _service.UpdateEmployeeAsync(p.Validate<Employee, EmployeeValidator>(), id));

    [HttpDelete("{id}", Name = "Delete")]
    public Task<IActionResult> DeleteAsync(Guid id)
        => _webApi.DeleteAsync(Request, _ => _service.DeleteEmployeeAsync(id));
```

<br/>

### Azure HTTP-triggered Function example

The following demonstrates usage when creating an Azure HTTP-triggered Function (essentially the same `_webApi` invocation code to `Controller` above):

``` csharp
public class EmployeeFunction
{
    private readonly WebApi _webApi;
    private readonly EmployeeService _service;

    public EmployeeFunction(WebApi webApi, EmployeeService service)
    {
        _webApi = webApi;
        _service = service;
    }

    [FunctionName("Get")]
    public Task<IActionResult> GetAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.GetAsync(request, _ => _service.GetEmployeeAsync(id));

    [FunctionName("GetAll")]
    public Task<IActionResult> GetAllAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/employees")] HttpRequest request)
        => _webApi.GetAsync(request, p => _service.GetAllAsync(p.RequestOptions.Paging));

    [FunctionName("Create")]
    public Task<IActionResult> CreateAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/employees")] HttpRequest request)
        => _webApi.PostAsync<Employee, Employee>(request, p => _service.AddEmployeeAsync(p.Validate<Employee, EmployeeValidator>()),
           statusCode: HttpStatusCode.Created, locationUri: e => new Uri($"api/employees/{e.Id}", UriKind.RelativeOrAbsolute));

    [FunctionName("Update")]
    public Task<IActionResult> UpdateAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.PutAsync<Employee, Employee>(request, p => _service.UpdateEmployeeAsync(p.Validate<Employee, EmployeeValidator>(), id));

    [FunctionName("Patch")]
    public Task<IActionResult> PatchAsync([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.PatchAsync(request, get: _ => _service.GetEmployeeAsync(id), put: p => _service.UpdateEmployeeAsync(p.Validate<Employee, EmployeeValidator>(), id));

    [FunctionName("Delete")]
    public Task<IActionResult> DeleteAsync([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.DeleteAsync(request, _ => _service.DeleteEmployeeAsync(id));
```

<br/>

## WebApiPublish

The [`WebApiPublisher`](./WebApis/WebApiPublisher.cs) class should be leveraged for _fire-and-forget_ style APIs, where the message is received, validated and then published as an event for out-of-process decoupled processing.

The `WebApiPublish` extends (inherits) [`WebApiBase`](./WebApis/WebApiBase.cs) that provides the base `RunAsync` method described [above](#WebApi).

The `WebApiPublisher` constructor takes an [`IEventPublisher`](../CoreEx/Events/IEventPublisher.cs) that is responsible for formatting and sending the event to the requisite messaging platform. See [Events](./CoreEx/Events) for more information regarding events.

<br/>

### Supported HTTP methods

A publish should be performed using an HTTP `POST` and as such this is the only HTTP method supported. The `WebApiPublish` provides the following overloads depending on need.

HTTP | Method | Description
-|-|-
`POST` | `PublishAsync<TValue>()` | Publish a single message/event with `TValue` being the request content body.
`POST` | `PublishValueAsync<TValue>()` | Publish a single message/event with `TValue` being the specified value (preivously deserialized).
`POST` | `PublishAsync<TValue, TEventValue>()` | Publish a single message/event with `TValue` being the request content body mapping to the specified event value type.
`POST` | `PublishValueAsync<TValue, TEventValue>()` | Publish a single message/event with `TValue` being the specified value (preivously deserialized) mapping to the specified event value type.
- | -
`POST` | `PublishCollectionAsync<TColl, TItem>()` | Publish zero or more message/event(s) from the `TColl` collection with an item type of `TItem` being the request content body.
`POST` | `PublishCollectionValueAsync<TColl, TItem>()` | Publish zero or more message/event(s) from the `TColl` collection with an item type of `TItem` being the specified value (preivously deserialized).
`POST` | `PublishCollectionAsync<TColl, TItem, TEventItem>()` | Publish zero or more message/event(s) from the `TColl` collection with an item type of `TItem` being the request content body mapping to the specified event value type.
`POST` | `PublishCollectionValueAsync<TColl, TItem, TEventItem>()` | Publish zero or more message/event(s) from the `TColl` collection with an item type of `TItem` being the specified value (preivously deserialized) mapping to the specified event value type.

<br/>

### Argument

Depending on the overload used (as defined above), an optional _argument_ can be specified that provides additional opportunities to configure and add additional logic into the underlying publishing orchestration.

The following argurment types are supported:
- [`WebApiPublisherArgs<TValue>`](./WebApis/WebApiPublisherArgsT.cs) - single message with no mapping.
- [`WebApiPublisherArgs<TValue, TEventValue>`](./WebApis/WebApiPublisherArgsT2.cs) - single message _with_ [mapping](https://github.com/Avanade/CoreEx/tree/main/src/CoreEx/Mapping).
- [`WebApiPublisherCollectionArgs<TColl, TItem>`](./WebApis/WebApiPublisherCollectionArgsT.cs) - collection of messages with no mapping.
- [`WebApiPublisherCollectionArgs<TColl, TItem, TEventItem>`](./WebApis/WebApiPublisherCollectionArgsT2.cs) - collection of messages _with_ [mapping](https://github.com/Avanade/CoreEx/tree/main/src/CoreEx/Mapping).

The arguments will have the following properties depending on the supported functionality. The sequence defines the order in which each of the properties is enacted (orchestrated) internally. Where a failure or exception occurs then the execution will be aborted and the corresponding `IActionResult` returned (including the likes of logging etc. where applicable).

Property | Description | Sequence
-|-
`EventName` | The event destintion name (e.g. Queue or Topic name) where applicable. | N/A
`EventTemplate` | The [`EventData`](../CoreEx/Events/EventData.cs) template to be used to create the message/event. | N/A
`StatusCode` | The resulting status code where successful. Defaults to `204-Accepted`. | N/A
`OperationType` | The [`OperationType`](../CoreEx/OperationType.cs). Defaults to `OperationType.Unspecified`. | N/A
`MaxCollectionSize` | The maximum collection size allowed/supported (where applicable). | 1
`OnBeforeValidateAsync` | The function to be invoked before the request value is validated; opportunity to modify contents. | 2
`Validator` | The `IValidator<T>` to validate the request value. | 3
`OnBeforeEventAsync` | The function to be invoked after validation / before event; opportunity to modify contents. | 4
`Mapper` | The `IMapper<TSource, TDestination>` override (where applicable). | 5
`OnEvent` | The action to be invoked once converted to an [`EventData`](../CoreEx/Events/EventData.cs); opportunity to modify contents. | 6
`CreateSuccessResult` | The function to be invoked to create/override the success `IActionResult`. Defaults to returning specified `StatusCode`. | 7

<br/>

### Request

A request body is mandatory and must be serialized JSON as per the specified generic types.

<br/>

### Response

The response HTTP status code is `204-Accepted` (default) with no content. This can be overridden using the arguments `StatusCode` property.

<br/>

### Azure HTTP-triggered Function example

The following demonstrates usage when creating an Azure HTTP-triggered Function:

``` csharp
public class HttpTriggerQueueVerificationFunction
{
    private readonly WebApiPublisher _webApiPublisher;
    private readonly HrSettings _settings;

    public HttpTriggerQueueVerificationFunction(WebApiPublisher webApiPublisher, HrSettings settings)
    {
        _webApiPublisher = webApiPublisher;
        _settings = settings;
    }

    public Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "employee/verify")] HttpRequest request)
        => _webApiPublisher.PublishAsync(request, new WebApiPublisherArgs<EmployeeVerificationRequest>(_settings.VerificationQueueName) { Validator = new EmployeeVerificationValidator().Wrap() });
}