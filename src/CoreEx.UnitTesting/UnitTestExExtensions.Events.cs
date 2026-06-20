#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class UnitTestExExtensions
{
    /// <summary>
    /// Replaces the registered <see cref="IEventPublisher"/> with a decorator (<see cref="EventPublisherDecorator"/>) that also captures the published events for expectation assertions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="bypassPassThrough">Indicates whether to bypass the pass-through to the original event publisher.</param>
    /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The originating <see cref="IEventPublisher"/> should have been registered with the <see cref="Microsoft.Extensions.DependencyInjection.CoreExEventsExtensions.AddEventPublisher(IServiceCollection, string, Func{IServiceProvider, IEventPublisher}, bool)"/>
    /// to ensure the service was registered correctly to enable this functionality.
    /// <para>The <paramref name="bypassPassThrough"/> when set to <see langword="true"/> will bypass the pass-through to the original event publisher and leverage the <see cref="NoOpEventPublisher"/> instead.</para></remarks>
    public static IServiceCollection UseExpectedEventPublisher(this IServiceCollection services, string serviceKey = "EventPublisher", bool bypassPassThrough = false)
        => services.ThrowIfNull().ReplaceKeyedScoped<IEventPublisher>(serviceKey.ThrowIfNullOrEmpty(), (sp, _) =>
        {
            var rootServiceKey = $"{serviceKey}_Root";
            var root = bypassPassThrough
                ? ActivatorUtilities.CreateInstance<NoOpEventPublisher>(sp)
                : sp.GetKeyedService<IEventPublisher>(rootServiceKey) ?? throw new InvalidOperationException($"The root '{rootServiceKey}' publisher must be registered before the expected publisher can be used.");

            var sharedState = sp.GetService<TestSharedState>() ?? throw new InvalidOperationException($"The UnitTestEx test shared state must be registered as required by the underlying {nameof(EventPublisherDecorator)}.");
            return new EventPublisherDecorator(serviceKey, sharedState, root);
        });
}