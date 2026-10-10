#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Models externally managed infrastructure as a visible connection-string resource backed by secret configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The existing connection-string name.</param>
    /// <param name="endpointKey">An optional connection-string key containing an absolute HTTP or HTTPS endpoint URL for dashboard peer matching.</param>
    /// <returns>The visible resource, without taking ownership of the infrastructure lifecycle.</returns>
    /// <remarks>Reads <c>ConnectionStrings:{name}</c> through a secret parameter. Unlike parameter-only <c>AddConnectionString(name)</c>,
    /// this resource appears in Aspire's Graph/Table views. Its running status indicates resolved configuration, not service health;
    /// hosts remain responsible for readiness probes. For Cosmos DB, specify <c>endpointKey: "AccountEndpoint"</c> because Aspire's
    /// connection-string parser does not recognise that key. Credentials are not permitted in the endpoint URL.</remarks>
    public static Aha.IResourceBuilder<ConnectionStringResource> AddExternalConnectionString(this IDistributedApplicationBuilder builder, string name, string? endpointKey = null)
    {
        builder.ThrowIfNull();
        name.ThrowIfNullOrEmpty();
        var parameter = builder.AddParameterFromConfiguration($"{name}-connection", $"ConnectionStrings:{name}", secret: true);
        var resource = builder.AddConnectionString(name, Aha.ReferenceExpression.Create($"{parameter}"));

        if (endpointKey is not null)
        {
            endpointKey.ThrowIfNullOrEmpty();
            var connectionString = builder.Configuration.GetConnectionString(name) ?? throw new InvalidOperationException($"Connection string '{name}' is required.");
            var properties = new DbConnectionStringBuilder { ConnectionString = connectionString };

            if (!properties.TryGetValue(endpointKey, out var endpoint) || endpoint is not string url || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException($"Connection string '{name}' must contain an absolute HTTP or HTTPS '{endpointKey}' URL without credentials.");

            resource.WithUrl(uri.GetLeftPart(UriPartial.Authority));
        }

        return resource;
    }

    /// <summary>
    /// Annotates the resource's <c>http</c> endpoint with one or more relative <paramref name="urls"/> so they appear as clickable links in the Aspire dashboard.
    /// </summary>
    /// <param name="builder">The <see cref="Aha.IResourceBuilder{T}"/> of <see cref="Aha.ProjectResource"/>.</param>
    /// <param name="urls">The relative URLs to add, e.g. <c>"/health/ready/detailed"</c>.</param>
    /// <returns>The <see cref="Aha.IResourceBuilder{T}"/> for fluent-style method-chaining.</returns>
    public static Aha.IResourceBuilder<Aha.ProjectResource> AddEndpoints(this Aha.IResourceBuilder<Aha.ProjectResource> builder, params string[] urls)
    {
        var httpEndpoint = builder.GetEndpoint("http");
        foreach (var url in urls)
        {
            builder.WithAnnotation(new Aha.ResourceUrlAnnotation { Endpoint = httpEndpoint, Url = url });
        }

        return builder;
    }

    /// <summary>
    /// Adds a dashboard command button that invokes an HTTP <paramref name="method"/> against a relative <paramref name="path"/> on the resource.
    /// </summary>
    /// <param name="builder">The <see cref="Aha.IResourceBuilder{T}"/> of <see cref="Aha.ProjectResource"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/> to invoke.</param>
    /// <param name="path">The relative path to invoke, e.g. <c>"/hosted-services/all/pause"</c>.</param>
    /// <param name="displayName">The button's display name shown in the dashboard.</param>
    /// <param name="iconName">The optional Fluent UI icon name for the button; see <see href="https://storybooks.fluentui.dev/react/?path=/docs/icons-catalog--docs">the icon catalog</see>.</param>
    /// <returns>The <see cref="Aha.IResourceBuilder{T}"/> for fluent-style method-chaining.</returns>
    public static Aha.IResourceBuilder<Aha.ProjectResource> AddCommand(this Aha.IResourceBuilder<Aha.ProjectResource> builder, HttpMethod method, string path, string displayName, string? iconName)
        => builder.WithHttpCommand(
            path: path,
            displayName: displayName,
            commandOptions: new Aha.HttpCommandOptions() { Method = method, IconName = iconName });

    /// <summary>
    /// Adds dashboard support for the standard <i>CoreEx</i> hosted-service management endpoints (<c>/hosted-services/all/{status,pause,resume}</c> via <c>MapHostedServices()</c>) -- a status link plus "Pause all services"/"Resume all services" command buttons.
    /// </summary>
    /// <param name="builder">The <see cref="Aha.IResourceBuilder{T}"/> of <see cref="Aha.ProjectResource"/>.</param>
    /// <returns>The <see cref="Aha.IResourceBuilder{T}"/> for fluent-style method-chaining.</returns>
    /// <remarks>The stock Api host template doesn't register <c>AddHostedServiceManager()</c>/<c>MapHostedServices()</c> -- Relay and Subscribe do. This split is a workload-isolation convention, not a technical requirement; for a small, low-traffic solution, consolidating hosted-service processing into the Api host is a reasonable simplification. Call this method wherever those endpoints are actually mapped.</remarks>
    public static Aha.IResourceBuilder<Aha.ProjectResource> AddHostedServiceSupport(this Aha.IResourceBuilder<Aha.ProjectResource> builder)
        => builder.AddEndpoints("/hosted-services/all/status")
            .AddCommand(HttpMethod.Post, "/hosted-services/all/pause", "Pause all services", "Pause")
            .AddCommand(HttpMethod.Post, "/hosted-services/all/resume", "Resume all services", "PauseOff");

    /// <summary>
    /// Disables TLS/SSL certificate validation for all outbound <see cref="HttpClient"/> instances created via <c>ConfigureHttpClientDefaults</c>.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IDistributedApplicationBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>Intended for local development and testing only, e.g. trusting self-signed certificates presented by emulators (Service Bus, Cosmos DB, etc.). It's also useful because <c>dotnet dev-certs https --trust</c> isn't fully supported on Linux, so an ASP.NET Core project resource's HTTPS endpoint may present a dev certificate that isn't OS-trusted (e.g. on Linux CI runners); anything resolving an <see cref="HttpClient"/> via this same DI container's <c>IHttpClientFactory</c> -- health check probes registered against a project resource's endpoint, or test-harness clients such as <c>AspireTesterBase.CreateHttpClient()</c> -- is covered by this call. This bypasses all certificate validation for every <see cref="HttpClient"/> in the app, so it must never be used in production.</remarks>
    public static IDistributedApplicationBuilder DisableHttpCertificateValidation(this IDistributedApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpClientDefaults(http =>
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            }));

        return builder;
    }
}
