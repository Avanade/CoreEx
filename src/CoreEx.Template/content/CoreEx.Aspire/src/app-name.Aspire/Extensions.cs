/// <summary>
/// Provides <see cref="IResourceBuilder{T}"/> extensions that add Aspire dashboard sugar (deep-linked endpoints
/// and command buttons) for the <see cref="ProjectResource"/>s this AppHost orchestrates.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Annotates the resource's <c>http</c> endpoint with one or more relative <paramref name="urls"/> so they appear as clickable links in the Aspire dashboard.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> of <see cref="ProjectResource"/>.</param>
    /// <param name="urls">The relative URLs to add, e.g. <c>"/health/ready/detailed"</c>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for fluent-style method-chaining.</returns>
    public static IResourceBuilder<ProjectResource> AddEndpoints(this IResourceBuilder<ProjectResource> builder, params string[] urls)
    {
        var httpEndpoint = builder.GetEndpoint("http");
        foreach (var url in urls)
        {
            builder.WithAnnotation(new ResourceUrlAnnotation { Endpoint = httpEndpoint, Url = url });
        }

        return builder;
    }

    /// <summary>
    /// Adds a dashboard command button that invokes an HTTP <paramref name="method"/> against a relative <paramref name="path"/> on the resource.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> of <see cref="ProjectResource"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/> to invoke.</param>
    /// <param name="path">The relative path to invoke, e.g. <c>"/hosted-services/all/pause"</c>.</param>
    /// <param name="displayName">The button's display name shown in the dashboard.</param>
    /// <param name="iconName">The optional Fluent UI icon name for the button; see <see href="https://storybooks.fluentui.dev/react/?path=/docs/icons-catalog--docs">the icon catalog</see>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for fluent-style method-chaining.</returns>
    public static IResourceBuilder<ProjectResource> AddCommand(this IResourceBuilder<ProjectResource> builder, HttpMethod method, string path, string displayName, string? iconName)
        => builder.WithHttpCommand(
            path: path,
            displayName: displayName,
            commandOptions: new HttpCommandOptions() { Method = method, IconName = iconName });

    /// <summary>
    /// Adds dashboard support for the standard <i>CoreEx</i> hosted-service management endpoints (<c>/hosted-services/all/{status,pause,resume}</c> via <c>MapHostedServices()</c>) -- a status link plus "Pause all services"/"Resume all services" command buttons.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/> of <see cref="ProjectResource"/>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/> for fluent-style method-chaining.</returns>
    /// <remarks>The stock Api host template doesn't register <c>AddHostedServiceManager()</c>/<c>MapHostedServices()</c> -- Relay and Subscribe do. This split is a workload-isolation convention, not a technical requirement; for a small, low-traffic solution, consolidating hosted-service processing into the Api host is a reasonable simplification. Call this method wherever those endpoints are actually mapped.</remarks>
    public static IResourceBuilder<ProjectResource> AddHostedServiceSupport(this IResourceBuilder<ProjectResource> builder)
        => builder.AddEndpoints("/hosted-services/all/status")
            .AddCommand(HttpMethod.Post, "/hosted-services/all/pause", "Pause all services", "Pause")
            .AddCommand(HttpMethod.Post, "/hosted-services/all/resume", "Resume all services", "PauseOff");
}
