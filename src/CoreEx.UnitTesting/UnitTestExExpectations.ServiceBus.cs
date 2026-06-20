#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace UnitTestEx.Expectations;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="CoreEx"/>-specific extension methods to <see cref="UnitTestEx"/>.
/// </summary>
public static partial class UnitTestExExpectations
{
    /// <summary>
    /// Expects that no events will have been published for the keyed <see cref="IEventPublisher"/> (<paramref name="serviceKey"/> defaults to <see cref="ServiceBusPublisher.DefaultServiceKey"/>).
    /// </summary>
    /// <param name="tester">The <see cref="IExpectations{TSelf}"/> tester.</param>
    /// <param name="serviceKey">The service key used for the keyed registration.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="serviceKey"/> must be the same as used when registering the underlying <see cref="IEventPublisher"/>.</remarks>
    public static TSelf ExpectNoAzureServiceBusEvents<TSelf>(this IExpectations<TSelf> tester, string serviceKey = ServiceBusPublisher.DefaultServiceKey) where TSelf : IExpectations<TSelf>
        => ExpectNoEvents(tester, serviceKey);

    /// <summary>
    /// Expects that events will have been published for the keyed <see cref="IEventPublisher"/> (<paramref name="serviceKey"/> defaults to <see cref="ServiceBusPublisher.DefaultServiceKey"/>).
    /// </summary>
    /// <param name="tester">The <see cref="IExpectations{TSelf}"/> tester.</param>
    /// <param name="configure">The action to enable events expectations configuration.</param>
    /// <param name="serviceKey">The service key used for the keyed registration.</param>
    /// <returns>The <typeparamref name="TSelf"/> instance to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="serviceKey"/> must be the same as used when registering the underlying <see cref="IEventPublisher"/>.</remarks>
    public static TSelf ExpectAzureServiceBusEvents<TSelf>(this IExpectations<TSelf> tester, Action<EventExpectationsConfig>? configure = null, string serviceKey = ServiceBusPublisher.DefaultServiceKey) where TSelf : IExpectations<TSelf>
        => ExpectEvents(tester, serviceKey, configure, Assembly.GetCallingAssembly());
}