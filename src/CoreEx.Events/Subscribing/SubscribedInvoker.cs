namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the <see cref="SubscribedBase"/> invoker.
/// </summary>
/// <remarks>This invoker is <i>only</i> used where a <see cref="SubscribedBase"/> is involved; i.e has been subscribed to.</remarks>
[InvokerName("CoreEx.Events.Subscribing.Subscribed")]
public class SubscribedInvoker : InvokerBase<SubscribedBase> { }