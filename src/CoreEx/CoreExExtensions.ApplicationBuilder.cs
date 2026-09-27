#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static class CoreExExtensions
{
    /// <summary>
    /// Adds a <b>singleton</b> <see cref="IHostSettings"/> service.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="solutionName">The solution name; for example: '<c>Contoso</c>.</param>
    /// <param name="domainName">The domain name; for example: '<c>Shopping</c>.</param>
    /// <param name="source">The source <see cref="Uri"/>; for example '<c>urn:contoso:products</c>'.</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for fluent-style method-chaining.</returns>
    public static IHostApplicationBuilder AddHostSettings(this IHostApplicationBuilder builder, string? solutionName = null, string? domainName = null, Uri? source = null)
    {
        builder.ThrowIfNull();

        var env = builder.Configuration.GetValue<string?>("CoreEx:Host:EnvironmentName")
            ?? builder.Configuration.GetValue<string?>("COREEX_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("COREEX_ENVIRONMENT")
            ?? builder.Environment.EnvironmentName;

        var hs = HostSettings.Create(builder.Configuration, env, solutionName, domainName, source);
        builder.Properties[nameof(HostSettings)] = hs;
        builder.Services.AddSingleton<IHostSettings>(hs);
        return builder;
    }

    /// <summary>
    /// Adds an opinionated <i>typed</i> <see cref="HttpClient"/> with idempotency key handler, service discovery, and standard resilience handlers.
    /// </summary>
    /// <typeparam name="TClient">The typed client.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="name">The name of the <see cref="HttpClient"/>.</param>
    /// <param name="configureClient">An optional action to configure the <see cref="HttpClient"/>.</param>
    /// <param name="configureIdempotency">An optional action to configure the <see cref="IdempotencyKeyHandler"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="name"/> also represents the configuration section name for the <see cref="HttpClient"/>; as a minimum define the <see cref="HttpClient.BaseAddress"/>.
    /// <para>The handlers are added, in order specified, as follows:
    /// <list type="bullet">
    /// <item><description><see cref="IdempotencyKeyHandler"/> - Adds an idempotency key to outgoing HTTP requests.</description></item>
    /// <item><description><see href="https://learn.microsoft.com/dotnet/core/extensions/service-discovery">Service discovery</see> - Resolves the <see cref="HttpClient.BaseAddress"/> host as a logical service name (e.g. <c>https://products-api</c>) against a <c>Services:{name}:{scheme}</c> configuration section (or, on supporting platforms, a native resolver) before falling back unchanged to a literal address if no matching entry is found - see <see href="https://www.nuget.org/packages/Microsoft.Extensions.ServiceDiscovery">Microsoft.Extensions.ServiceDiscovery</see>. This makes an Aspire-orchestrated <c>BaseAddress</c> (populated via the AppHost's <c>WithReference</c>) and a literal production URL both work unchanged through the same code path - no Aspire AppHost is required at runtime for the latter.</description></item>
    /// <item><description><see href="https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience">Standard resilience handlers</see> - Adds standard resilience policies as enabled by <see href="https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience">Microsoft.Extensions.Http.Resilience</see>.</description></item>
    /// </list>
    /// </para>
    /// <para>Example configuration section for a <see cref="HttpClient"/> named '<c>ProductsApi</c>' pointing to a literal production address:
    /// <code>
    /// {
    ///   "ProductsApi": {
    ///     "BaseAddress": "https://api.contoso.com/",
    ///     "Resilience": {
    ///       ... // Resilience configuration as per Microsoft.Extensions.Http.Resilience documentation.
    ///     }
    ///   }
    /// }
    /// </code>
    /// Or, when consumed from within an Aspire AppHost (local development/testing), pointing to the logical Aspire resource name (e.g. <c>products-api</c>) instead of a hardcoded host/port - the AppHost's <c>WithReference(productsApi)</c> then supplies the actual resolved endpoint(s) via the <c>Services:products-api:*</c> configuration section regardless of which port the referenced project ultimately binds to:
    /// <code>
    /// {
    ///   "ProductsApi": {
    ///     "BaseAddress": "https+http://products-api"
    ///   }
    /// }
    /// </code>
    /// </para></remarks>
    public static IHttpClientBuilder AddTypedHttpClient<TClient>(this IHostApplicationBuilder builder, string name, Action<HttpClient>? configureClient = null, Action<IServiceProvider, IdempotencyKeyHandler>? configureIdempotency = null)
        where TClient : class
    {
        var config = builder.ThrowIfNull().Configuration.GetSection(name) ?? throw new ArgumentException($"Unable to find configuration section for '{name}'.");

        // Registers the underlying service endpoint resolver providers (configuration-based + pass-through); safe/idempotent to call more than once per host.
        builder.Services.AddServiceDiscovery();

        var cb = builder.Services.AddHttpClient(name, client =>
        {
            // Set the standard configured setting.
            client.BaseAddress = config.GetValue<Uri?>("BaseAddress") ?? throw new ArgumentException($"Unable to find '{nameof(HttpClient.BaseAddress)}' configuration for '{name}'.");
            configureClient?.Invoke(client);
        });

        cb.AddIdempotencyKeyHandler((sp, handler) => configureIdempotency?.Invoke(sp, handler));
        cb.AddServiceDiscovery();

        if (config.GetSection("Resilience").Exists())
            cb.AddStandardResilienceHandler(config);
        else
            cb.AddStandardResilienceHandler();

        cb.AddTypedClient<TClient>();

        return cb;
    }

    /// <summary>
    /// Adds an opinionated <i>typed</i> <see cref="HttpClient"/> with idempotency key handler and standard resilience handlers.
    /// </summary>
    /// <typeparam name="TClient">The typed client.</typeparam>
    /// <typeparam name="TImplementation">The the typed client implementation.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="name">The name of the <see cref="HttpClient"/>.</param>
    /// <param name="configureClient">An optional action to configure the <see cref="HttpClient"/>.</param>
    /// <param name="configureIdempotency">An optional action to configure the <see cref="IdempotencyKeyHandler"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="name"/> also represents the configuration section name for the <see cref="HttpClient"/>; as a minimum define the <see cref="HttpClient.BaseAddress"/>.
    /// <para>The handlers are added, in order specified, as follows:
    /// <list type="bullet">
    /// <item><description><see cref="IdempotencyKeyHandler"/> - Adds an idempotency key to outgoing HTTP requests.</description></item>
    /// <item><description><see href="https://learn.microsoft.com/dotnet/core/extensions/service-discovery">Service discovery</see> - Resolves the <see cref="HttpClient.BaseAddress"/> host as a logical service name (e.g. <c>https://products-api</c>) against a <c>Services:{name}:{scheme}</c> configuration section (or, on supporting platforms, a native resolver) before falling back unchanged to a literal address if no matching entry is found - see <see href="https://www.nuget.org/packages/Microsoft.Extensions.ServiceDiscovery">Microsoft.Extensions.ServiceDiscovery</see>. This makes an Aspire-orchestrated <c>BaseAddress</c> (populated via the AppHost's <c>WithReference</c>) and a literal production URL both work unchanged through the same code path - no Aspire AppHost is required at runtime for the latter.</description></item>
    /// <item><description><see href="https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience">Standard resilience handlers</see> - Adds standard resilience policies as enabled by <see href="https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience">Microsoft.Extensions.Http.Resilience</see>.</description></item>
    /// </list>
    /// </para>
    /// <para>Example configuration section for a <see cref="HttpClient"/> named '<c>ProductsApi</c>' pointing to a literal production address:
    /// <code>
    /// {
    ///   "ProductsApi": {
    ///     "BaseAddress": "https://api.contoso.com/",
    ///     "Resilience": {
    ///       ... // Resilience configuration as per Microsoft.Extensions.Http.Resilience documentation.
    ///     }
    ///   }
    /// }
    /// </code>
    /// Or, when consumed from within an Aspire AppHost (local development/testing), pointing to the logical Aspire resource name (e.g. <c>products-api</c>) instead of a hardcoded host/port - the AppHost's <c>WithReference(productsApi)</c> then supplies the actual resolved endpoint(s) via the <c>Services:products-api:*</c> configuration section regardless of which port the referenced project ultimately binds to:
    /// <code>
    /// {
    ///   "ProductsApi": {
    ///     "BaseAddress": "https+http://products-api"
    ///   }
    /// }
    /// </code>
    /// </para></remarks>
    public static IHttpClientBuilder AddTypedHttpClient<TClient, TImplementation>(this IHostApplicationBuilder builder, string name, Action<HttpClient>? configureClient = null, Action<IServiceProvider, IdempotencyKeyHandler>? configureIdempotency = null)
        where TClient : class where TImplementation : class, TClient
    {
        var config = builder.ThrowIfNull().Configuration.GetSection(name) ?? throw new ArgumentException($"Unable to find configuration section for '{name}'.");

        // Registers the underlying service endpoint resolver providers (configuration-based + pass-through); safe/idempotent to call more than once per host.
        builder.Services.AddServiceDiscovery();

        var cb = builder.Services.AddHttpClient(name, client =>
        {
            // Set the standard configured setting.
            client.BaseAddress = config.GetValue<Uri?>("BaseAddress") ?? throw new ArgumentException($"Unable to find '{nameof(HttpClient.BaseAddress)}' configuration for '{name}'.");
            configureClient?.Invoke(client);
        });

        cb.AddIdempotencyKeyHandler((sp, handler) => configureIdempotency?.Invoke(sp, handler));
        cb.AddServiceDiscovery();

        if (config.GetSection("Resilience").Exists())
            cb.AddStandardResilienceHandler(config);
        else
            cb.AddStandardResilienceHandler();

        cb.AddTypedClient<TClient, TImplementation>();

        return cb;
    }
}