namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Provides the standard <see cref="CosmosDbOutboxRelayProcessor"/> invoker functionality.
/// </summary>
/// <remarks>Deliberately has no overrides - tracing stays enabled (unlike <see cref="CosmosDbInvoker"/>, which disables it for high-frequency CRUD operations), mirroring
/// <c>CoreEx.Database.Outbox</c>'s <c>DatabaseOutboxRelayInvoker</c>, which is also a bare <see cref="InvokerBase{TCaller}"/> with only an <see cref="InvokerNameAttribute"/>. Relay batch processing is
/// comparatively low-frequency and specifically where distributed-tracing visibility (see <see cref="CoreEx.Events.CloudEventTracingExtensions"/>) is most valuable.</remarks>
[InvokerName("CoreEx.Cosmos.Outbox.Relay")]
public class CosmosDbOutboxRelayInvoker : InvokerBase<CosmosDbOutboxRelayProcessor>
{
    private static CosmosDbOutboxRelayInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="CosmosDbOutboxRelayInvoker"/> instance.
    /// </summary>
    public static CosmosDbOutboxRelayInvoker Default => ExecutionContext.GetService<CosmosDbOutboxRelayInvoker>() ?? (_default ??= new CosmosDbOutboxRelayInvoker());
}
