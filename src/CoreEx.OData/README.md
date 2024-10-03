# CoreEx

The `CoreEx.OData` namespace provides extended [_OData v4_](https://en.wikipedia.org/wiki/Open_Data_Protocol) support leveraging the [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client/wiki) open-source capabilities. 

<br/>

## Motivation

The motivation is to simplify and unify the approach to _OData_ access. The [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client/wiki) provides, as the name implies, a simple and easy to use _OData_ client.

<br/>

## ODataClient

The [`ODataClient`](./ODataClient.cs) class is a wrapper around the [`Simple.OData.Client.ODataClient`](https://github.com/simple-odata-client/Simple.OData.Client/wiki/Getting-started-with-Simple.OData.Client). Yes, they have the same name; sadly, naming stuff is [hard](https://martinfowler.com/bliki/TwoHardThings.html).

The [`ODataClient`](./ODataClient.cs) is the base (common) implementation for the [`IOData`](./IOData.cs) interface that provides the standardized access to the underlying endpoint. For typed access an [IMapper](../CoreEx/Mapping/IMapper.cs) that contains the mapping logic to map to and from the **entity** and underlying **model** ia required.

<br/>

## Requirements

The requirements for usage are as follows.
- An **entity** (DTO) that represents the data that must as a minimum implement [`IEntityKey`](../CoreEx/Entities/IEntityKey.cs); generally via either the implementation of [`IIdentifier`](../CoreEx/Entities/IIdentifierT.cs) or [`IPrimaryKey`](../CoreEx/Entities/IPrimaryKey.cs).
- A **model** being the underlying configured JSON-serializable representation of the data source model.
- An [`IMapper`](../CoreEx/Mapping/IMapper.cs) that contains the mapping logic to map to and from the **entity** and **model**.

The **entity** and **model** are different types to encourage separation between the externalized **entity** representation and the underlying **model**; which may be shaped differently, and have different property to column naming conventions, etc.

Additionally, untyped **model** access is also supported via an [`ODataItem`](./ODataItem.cs) (dictionary-based representation) and the [`ODataItemCollection`](./ODataItemCollection.cs), a CRUD-enabler for untyped.

<br/>

## CRUD capabilities

The [`IOData`](./IOData.cs) and corresponding [`ODataClient`](./ODataClient.cs) provide the base CRUD capabilities as follows:

<br/>

### Query (read)

A query is actioned using the [`ODataQuery`](./ODataQuery.cs) which is ostensibly a lighweight wrapper over an `IBoundClient<TModel>`(https://github.com/simple-odata-client/Simple.OData.Client/blob/master/src/Simple.OData.Client.Core/Fluent/IBoundClient.cs) that automatically maps from the **model** to the **entity**.

The following methods provide additional capabilities:

Method | Description
-|-
`WithPaging` | Adds `Skip` and `Take` paging to the query.
`SelectSingleAsync`, `SelectSingleWithResult` | Selects a single item.
`SelectSingleOrDefaultAsync`, `SelectSingleOrDefaultWithResultAsync` | Selects a single item or default.
`SelectFirstAsync`, `SelectFirstWithResultAsync` | Selects first item.
`SelectFirstOrDefaultAsync`, `SelectFirstOrDefaultWithResultAsync` | Selects first item or default.
`SelectQueryAsync`, `SelectQueryWithResultAsync` | Select items into or creating a resultant collection.
`SelectResultAsync`, `SelectResultWithResultAsync` | Select items creating a [`ICollectionResult`](../CoreEx/Entities/ICollectionResultT2.cs) which also contains corresponding [`PagingResult`](../CoreEx/Entities/PagingResult.cs).

<br/>

### Get (read)

Gets (`GetAsync` or `GetWithResultAsync`) the **entity** for the specified key mapping from the **model**. Uses [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client/wiki/Retrieving-a-single-row-by-key) internally to get the **model** using the specified key.

<br/>

### Create 

Creates (`CreateAsync` or `CreateWithResultAsync`) the **entity** by firstly mapping to the **model**. Uses [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client/wiki/Adding-entries) to insert.

Where the **entity** implements [`IChangeLogAuditLog`](../CoreEx/Entities/IChangeLogAuditLog.cs) generally via [`ChangeLog`](../CoreEx/Entities/IChangeLog.cs) or [`ChangeLogEx`](../CoreEx/Entities/Extended/IChangeLogEx.cs), then the `CreatedBy` and `CreatedDate` properties will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Where the **entity** and/or **model** implements [`ITenantId`](../CoreEx/Entities/ITenantId.cs) then the `TenantId` property will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

<br/>

### Update

Updates (`UpdateAsync` or `UpdateWithResultAsync`) the **entity** by firstly mapping to the **model**. Uses [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client/wiki/Updating-entries) to update.

Where the **entity** implements [`IChangeLogAuditLog`](../CoreEx/Entities/IChangeLogAuditLog.cs) generally via [`ChangeLog`](../CoreEx/Entities/IChangeLog.cs) or [`ChangeLogEx`](../CoreEx/Entities/Extended/IChangeLogEx.cs), then the `UpdatedBy` and `UpdatedDate` properties will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

Where the **entity** and/or **model** implements [`ITenantId`](../CoreEx/Entities/ITenantId.cs) then the `TenantId` property will be automatically set from the [`ExecutionContext`](../CoreEx/ExecutionContext.cs).

<br/>

### Delete

Deletes (`DeleteAsync` or `DeleteWithResultAsync`) the **entity**. Uses [`Simple.OData.Client`](https://github.com/simple-odata-client/Simple.OData.Client/wiki/Deleting-entries) to delete.

<br/>

## Untyped

Untyped refers to the support of a **model** that is not defined as a type at compile time; is from a `Simple.OData.Client.ODataClient` perspective a `IDictionary<string, object>`. The [`ODataItem`](./ODataItem.cs) encapsulates the dictionary-based representation with the corresponding [`ODataItemCollection`](./ODataItemCollection.cs) enabling CRUD operations.

The [`ODataMapper`](./Mapping/ODataMapperT.cs) provides the mapping logic to map to and from the **entity** and untyped **model** dictionary. The following demonstrates:

``` csharp
public class CustomerToDataverseAccountMapper : ODataMapper<Customer>
{
    public CustomerToDataverseAccountMapper()
    {
        Property(c => c.AccountId, "accountid", OperationTypes.AnyExceptCreate).SetPrimaryKey();
        Property(x => x.FirstName, "firstname");
        Property(x => x.LastName, "lastname");
    }
}
```

<br/>

## Usage

To use the [`Simple.OData.Client.ODataClient`](https://github.com/simple-odata-client/Simple.OData.Client/wiki/Getting-started-with-Simple.OData.Client) must first be instantiated, then passed to the [`ODataClient`](./ODataClient.cs) constructor including a reference to the [`IMapper`](../CoreEx/Mapping/IMapper.cs).

The following will demonstrate the usage connecting to [_Microsoft Dataverse Web API_](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview) within the context of solution leveraging the _CoreEx_ library and dependency injection (DI).

<br/>

### Settings

The _Dataverse_ connection string settings are required:

``` csharp
public class DemoSettings : SettingsBase
{
    private readonly string UrlKey = "Url";
    private readonly string ClientIdKey = "ClientId";
    private readonly string CiientSecretKey = "ClientSecret";
    private readonly string TenantIdKey = "TenantId";

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoSettings"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    public DemoSettings(IConfiguration configuration) : base(configuration, "Demo") { }

    /// <summary>
    /// Gets the Dataverse connection string.
    /// </summary>
    public string DataverseConnectionString => GetRequiredValue<string>("ConnectionStrings__Dataverse");

    /// <summary>
    /// Gets the <see cref="DataverseSettings"/> from the <see cref="DataverseConnectionString"/>.
    /// </summary>
    public DataverseSettings DataverseConnectionSettings
    {
        get
        {
            var cs = DataverseConnectionString.Split(';').Select(s => s.Split('=')).ToDictionary(s => s[0].Trim(), s => s[1].Trim(), StringComparer.OrdinalIgnoreCase);
            if (!cs.TryGetValue(UrlKey, out var url)) throw new InvalidOperationException($"The connection string is missing the '{UrlKey}' key.");
            if (!cs.TryGetValue(ClientIdKey, out var clientId)) throw new InvalidOperationException($"The connection string is missing the '{ClientIdKey}' key.");
            if (!cs.TryGetValue(CiientSecretKey, out var clientSecret)) throw new InvalidOperationException($"The connection string is missing the '{CiientSecretKey}' key.");
            if (!cs.TryGetValue(TenantIdKey, out var tenantId)) throw new InvalidOperationException($"The connection string is missing the '{TenantIdKey}' key.");
            return new DataverseSettings(url, clientId, clientSecret, tenantId);
        }
    }

    /// <summary>
    /// Gets the Dataverse OData endpoint from the <see cref="DataverseConnectionString"/>.
    /// </summary>
    public Uri DataverseODataEndpoint => new(DataverseConnectionSettings.Address, "/api/data/v9.2/");

    /// <summary>
    /// Represents the resuluting <see cref="DataverseConnectionSettings"/>.
    /// </summary>
    public class DataverseSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseSettings"/> class.
        /// </summary>
        internal DataverseSettings(string url, string clientId, string clientSecret, string tenantId)
        {
            Address = new Uri(url);
            ClientId = clientId;
            ClientSecret = clientSecret;
            TenantId = tenantId;
        }

        /// <summary>
        /// Gets the address <see cref="Uri"/>.
        /// </summary>
        public Uri Address { get; }

        /// <summary>
        /// Gets the client identifier.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the client secret.
        /// </summary>
        public string ClientSecret { get; } 

        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public string TenantId { get; }
    }
}
```

The _Dataverse_ connection string is stored in the `appsettings.json` file:

``` json
{
  "ConnectionStrings": {
	"Dataverse": "Url=https://<your-tenant>.crm.dynamics.com;ClientId=<your-client-id>;ClientSecret=<your-client-secret>;TenantId=<your-tenant-id>"
  }
}
```

<br/>

### Authentication

The _Dataverse_ authentication is handled by leveraging a [`DelegatingHandler`](https://learn.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers) to perform the authentication (uses [MSAL.NET](https://learn.microsoft.com/en-us/entra/msal/dotnet/getting-started/instantiate-confidential-client-config-options)), cache the token, and add the `Authorization` header to each request.

``` csharp
public class DataverseAuthenticationHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SyncSettings _settings;
    private AuthenticationResult? _authResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataverseAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="settings">The <see cref="SyncSettings"/>.</param>
    public DataverseAuthenticationHandler(SyncSettings settings) => _settings = settings.ThrowIfNull();

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Verify and renew token if needed.
        await VerifyAndRenewTokenAsync(cancellationToken).ConfigureAwait(false);

        // Set the authorization bearer token.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authResult!.AccessToken);

        // Honor the commitment to keep calling down the chain.
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies and renews the token if needed: first time, has expired, or will expire in less than 5 minutes - then get token and cache for improved performance.
    /// </summary>
    private async Task VerifyAndRenewTokenAsync(CancellationToken cancellationToken)
    {
        // First time, has expired, or will expire in less than 5 minutes, then get token - token cached for performance.
        var expiryLimit = DateTimeOffset.UtcNow.AddMinutes(5);
        if (_authResult == null || _authResult.ExpiresOn <= expiryLimit)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Recheck in case another thread has already renewed the token.
                if (_authResult == null || _authResult.ExpiresOn <= expiryLimit)
                {
                    var dcs = _settings.DataverseConnectionSettings;
                    var authority = new Uri($"https://login.microsoftonline.com/{dcs.TenantId}");

                    var app = ConfidentialClientApplicationBuilder
                        .Create(dcs.ClientId)
                        .WithClientSecret(dcs.ClientSecret)
                        .WithAuthority(authority)
                        .Build();

                    var scopes = new List<string> { new Uri(dcs.Address, "/.default").AbsoluteUri };
                    _authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
```

<br/>

### Dataverse Client

Extend to the [`ODataClient`](./ODataClient.cs) to provide the specific _Dataverse_ implementation. The [`ODataArgs`](./ODataArgs.cs) can be used to configure the `ODataClient` to derive specific behavior where applicable.

``` csharp
using Soc = Simple.OData.Client;

public class DataverseClient : ODataClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataverseClient"/> class.
    /// </summary>
    /// <param name="client">The <see cref="Soc.ODataClient"/>.</param>
    /// <param name="mapper">The <see cref="IMapper"/>.</param>
    public DataverseClient(Soc.ODataClient client, IMapper mapper) : base(client, mapper)
    {
        Args = new ODataArgs { PreReadOnUpdate = false };
    }
}
```

### Registration

At application start up the dependency injection (DI) needs to be configured; comprised of the following:
- Register the `DataverseClient` as scoped; assumes that the `IMapper` has also been configured.
- Register the `Soc.ODataClient` as scoped; instantiates a new `Soc.ODataClientSettings` with the _named_ `HttpClient`.
- Register the `DataverseAuthenticationHandler` as a singleton; to ensure the underlying token is used for all requests.
- Register the `HttpClient` with a name of `"dataverse"`; also configured with the `DataverseAuthenticationHandler`.

``` csharp
// Configure the Dataverse required services.
Services
    .AddScoped<DataverseClient>()
    .AddScoped(sp =>
    {
        var hc = sp.GetRequiredService<IHttpClientFactory>().CreateClient("dataverse");
        var socs = new Soc.ODataClientSettings(hc);
        return new Soc.ODataClient(socs);
    })
    .AddSingleton<DataverseAuthenticationHandler>() // Singleton to ensure the underlying token is reused.
    .AddHttpClient("dataverse", (sp, client) => client.BaseAddress = sp.GetRequiredService<SyncSettings>().DataverseODataEndpoint)
        .AddHttpMessageHandler<DataverseAuthenticationHandler>();
```