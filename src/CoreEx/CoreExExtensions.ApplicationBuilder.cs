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
    /// Adds an opinionated <i>typed</i> <see cref="HttpClient"/> with idempotency key handler and standard resilience handlers.
    /// </summary>
    /// <typeparam name="TClient">The typed client.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="name">The name of the <see cref="HttpClient"/>.</param>
    /// <param name="configureClient">An optional action to configure the <see cref="HttpClient"/>.</param>
    /// <param name="configureIdempotency">An optional action to configure the <see cref="IdempotencyKeyHandler"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="name"/> also represents the configuration section name for the <see cref="HttpClient"/>; as a minimum define the <see cref="HttpClient.BaseAddress"/>.
    /// <para>The two handlers added are added, in order specified, as follows:
    /// <list type="bullet">
    /// <item><description><see cref="IdempotencyKeyHandler"/> - Adds an idempotency key to outgoing HTTP requests.</description></item>
    /// <item><description><see href="https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience">Standard resilience handlers</see> - Adds standard resilience policies as enabled by <see href="https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience">Microsoft.Extensions.Http.Resilience</see>.</description></item>
    /// </list>
    /// </para>
    /// <para>Example configuration section for a <see cref="HttpClient"/> named '<c>ProductsApi</c>':
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
    /// </para></remarks>
    public static IHttpClientBuilder AddTypedHttpClient<TClient>(this IHostApplicationBuilder builder, string name, Action<HttpClient>? configureClient = null, Action<IServiceProvider, IdempotencyKeyHandler>? configureIdempotency = null)
        where TClient : class
    {
        var config = builder.Configuration.GetSection(name) ?? throw new ArgumentException($"Unable to find configuration section for '{name}'.");

        var cb = builder.ThrowIfNull().Services.AddHttpClient(name, client =>
        {
            // Set the standard configured setting.
            client.BaseAddress = config.GetValue<Uri?>("BaseAddress") ?? throw new ArgumentException($"Unable to find '{nameof(HttpClient.BaseAddress)}' configuration for '{name}'.");
            configureClient?.Invoke(client);
        });

        cb.AddIdempotencyKeyHandler((sp, handler) => configureIdempotency?.Invoke(sp, handler));

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
    /// <para>The two handlers added are added, in order specified, as follows:
    /// <list type="bullet">
    /// <item><description><see cref="IdempotencyKeyHandler"/> - Adds an idempotency key to outgoing HTTP requests.</description></item>
    /// <item><description><see href="https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience">Standard resilience handlers</see> - Adds standard resilience policies as enabled by <see href="https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience">Microsoft.Extensions.Http.Resilience</see>.</description></item>
    /// </list>
    /// </para>
    /// <para>Example configuration section for a <see cref="HttpClient"/> named '<c>ProductsApi</c>':
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
    /// </para></remarks>
    public static IHttpClientBuilder AddTypedHttpClient<TClient, TImplementation>(this IHostApplicationBuilder builder, string name, Action<HttpClient>? configureClient = null, Action<IServiceProvider, IdempotencyKeyHandler>? configureIdempotency = null)
        where TClient : class where TImplementation : class, TClient
    {
        var config = builder.Configuration.GetSection(name) ?? throw new ArgumentException($"Unable to find configuration section for '{name}'.");

        var cb = builder.ThrowIfNull().Services.AddHttpClient(name, client =>
        {
            // Set the standard configured setting.
            client.BaseAddress = config.GetValue<Uri?>("BaseAddress") ?? throw new ArgumentException($"Unable to find '{nameof(HttpClient.BaseAddress)}' configuration for '{name}'.");
            configureClient?.Invoke(client);
        });

        cb.AddIdempotencyKeyHandler((sp, handler) => configureIdempotency?.Invoke(sp, handler));

        if (config.GetSection("Resilience").Exists())
            cb.AddStandardResilienceHandler(config);
        else
            cb.AddStandardResilienceHandler();

        cb.AddTypedClient<TClient, TImplementation>();

        return cb;
    }
}