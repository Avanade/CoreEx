#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx.Expectations;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="CoreEx"/>-specific extension methods to <see cref="UnitTestEx"/>.
/// </summary>
public static partial class UnitTestExExpectations
{
    /// <summary>
    /// Gets the <see cref="TestSharedState.RequestStateData(string?)"/> key suffix.
    /// </summary>
    internal const string RequestStateDataKeySuffix = "_Expectations";   

    /// <summary>
    /// Expects that no events will have been published for the keyed <see cref="IEventPublisher"/> (<paramref name="serviceKey"/> or default where not specified).
    /// </summary>
    /// <param name="tester">The <see cref="IExpectations{TSelf}"/> tester.</param>
    /// <param name="serviceKey">The service key used for the keyed registration.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="serviceKey"/> must be the same as used when registering the underlying <see cref="IEventPublisher"/>.</remarks>
    public static TSelf ExpectNoEvents<TSelf>(this IExpectations<TSelf> tester, string serviceKey) where TSelf : IExpectations<TSelf>
    {
        var requestId = tester.ExpectationsArranger.Tester is HttpTesterBase httpTester ? httpTester.RequestId : null;
        tester.ExpectationsArranger.GetOrAdd(() => new EventExpectations<TSelf>(tester.ExpectationsArranger.Owner, (TSelf)tester, requestId, Assembly.GetCallingAssembly())).ExpectNoEvents(serviceKey);

        // Tag that an expectation has been added for the current request (if applicable) so that the expectations will be evaluated at the end of the request.
        tester.ExpectationsArranger.Owner.SharedState.RequestStateData(requestId)[$"{serviceKey}_Expectations"] = null;
        return (TSelf)tester;
    }

    /// <summary>
    /// Expects that events will have been published for the keyed <see cref="IEventPublisher"/> (<paramref name="serviceKey"/> or default where not specified).
    /// </summary>
    /// <param name="tester">The <see cref="IExpectations{TSelf}"/> tester.</param>
    /// <param name="serviceKey">The service key used for the keyed registration.</param>
    /// <param name="configure">The action to enable events expectations configuration.</param>
    /// <param name="resourceAssembly">The optional <see cref="Assembly"/> to use for default resource resolution.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="serviceKey"/> must be the same as used when registering the underlying <see cref="IEventPublisher"/>.</remarks>
    public static TSelf ExpectEvents<TSelf>(this IExpectations<TSelf> tester, string serviceKey, Action<EventExpectationsConfig>? configure = null, Assembly? resourceAssembly = null) where TSelf : IExpectations<TSelf>
    {
        var requestId = tester.ExpectationsArranger.Tester is HttpTesterBase httpTester ? httpTester.RequestId : null;
        tester.ExpectationsArranger.GetOrAdd(() => new EventExpectations<TSelf>(tester.ExpectationsArranger.Owner, (TSelf)tester, requestId, resourceAssembly ?? Assembly.GetCallingAssembly())).ExpectEvents(serviceKey, configure);

        // Tag that an expectation has been added for the current request (if applicable) so that the expectations will be evaluated at the end of the request.
        tester.ExpectationsArranger.Owner.SharedState.RequestStateData(requestId)[$"{serviceKey}{RequestStateDataKeySuffix}"] = null;
        return (TSelf)tester;
    }

    /// <summary>
    /// Adds a post-run action to evaluate and clean up the captured events for the keyed <see cref="IEventPublisher"/> (<paramref name="serviceKey"/>).
    /// </summary>
    /// <typeparam name="TEntryPoint">The API startup <see cref="Type"/>.</typeparam>
    /// <param name="tester">The <see cref="AspNetCore.ApiTester{TEntryPoint}"/>.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="expectNoEvents">Indicates whether to expect no events to be published.</param>
    /// <returns>The <see cref="AspNetCore.ApiTester{TEntryPoint}"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="expectNoEvents"/> parameter is only actioned when no explicit event expectations are defined for the underlying test; acts as a catch all.
    /// <para>Note: this should not generally be invoked directly, but rather through a parent extension method, or equivalent, where applicable.</para></remarks>
    public static AspNetCore.ApiTester<TEntryPoint> AddEventExpectationsPostRun<TEntryPoint>(this AspNetCore.ApiTester<TEntryPoint> tester, string serviceKey, bool expectNoEvents = true) where TEntryPoint : class => tester
        .AddPostRunAfterExpectationsAction(instance =>
        {
            // Determine whether the test run itself had any expectations defined for the captured events; if not, then perform an expectation to ensure that no events were published (as the captured events would be unexpected).
            var requestId = instance is HttpTesterBase httpTester ? httpTester.RequestId : null;
            var data = tester.SharedState.RequestStateData(requestId);

            // If no explicit event expectations were defined for the test, then assert that no events were published (as any captured events would be unexpected).
            var eventKey = $"{serviceKey}{UnitTestExExpectations.RequestStateDataKeySuffix}";
            if (expectNoEvents && !data.ContainsKey(eventKey))
            {
                tester.Implementor.WriteLine("");
                tester.Implementor.WriteLine($"** '{serviceKey}' has no explicit event expectations defined; asserting that no events were published.");
                if (data.TryGetValue(serviceKey, out var obj) && obj is DestinationEvent[] events && events.Length > 0)
                    tester.Implementor.AssertFail($"Expected no {serviceKey} events; however, {events.Length} found to be published.");
                else
                    tester.Implementor.WriteLine("> Expected no events and there were none.");
            }
        })
        .AddPostRunAction(_ =>
        {
            // Pre-delete any existing captured events to ensure a clean slate for this test run.
            var eventKey = $"{serviceKey}{UnitTestExExpectations.RequestStateDataKeySuffix}";
            tester.SharedState.StateData.Remove(eventKey, out var __);
        });

    /// <summary>
    /// Configures an <paramref name="action"/> that will be executed (during the underlying <see cref="EventPublisherDecorator.PublishAsync"/>) for the keyed <see cref="IEventPublisher"/> (<paramref name="serviceKey"/>).
    /// </summary>
    /// <param name="tester">The <see cref="HttpTesterBase{TSelf}"/> tester.</param>
    /// <param name="serviceKey">The service key for the previously registered <see cref="IEventPublisher"/>.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The <see cref="HttpTesterBase{TSelf}"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>This allows for test-specific behaviors to be executed just prior to the actual publishing of events; i.e. simulating the throwing on an unexpected exception.</remarks>
    public static TSelf OnEventPublish<TSelf>(this TSelf tester, string serviceKey, Action action) where TSelf : HttpTesterBase<TSelf>
    {
        tester.Owner.SharedState.RequestStateData(tester.RequestId)[$"_{nameof(EventPublisherDecorator)}_{serviceKey}"] = action;
        return (TSelf)tester;
    }
}