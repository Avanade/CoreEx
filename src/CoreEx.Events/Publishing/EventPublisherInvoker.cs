namespace CoreEx.Events.Publishing;

/// <summary>
/// Provides the <see cref="EventPublisherBase"/> invoker.
/// </summary>
[InvokerName("CoreEx.Events.Publishing.EventPublisher")]
public class EventPublisherInvoker : InvokerBase<EventPublisherBase>
{
    private static EventPublisherInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="EventPublisherInvoker"/> instance.
    /// </summary>
    public static EventPublisherInvoker Default => ExecutionContext.GetService<EventPublisherInvoker>() ?? (_default ??= new EventPublisherInvoker());
}