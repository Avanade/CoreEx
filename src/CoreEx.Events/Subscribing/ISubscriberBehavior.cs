namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides a cross-cutting behavior that wraps subscriber processing for each delivery attempt and optionally observes the final settlement.
/// </summary>
/// <remarks>Behaviors are registered per subscriber using <see cref="EventSubscriberBase.Behaviors"/> and run outermost-first in registration order. A behavior must never settle the message itself; it influences
/// settlement only through the <see cref="Result"/> it returns.</remarks>
public interface ISubscriberBehavior
{
    /// <summary>
    /// Invokes the behavior for a single delivery attempt.
    /// </summary>
    /// <param name="args">The per-delivery <see cref="EventSubscriberArgs"/>; reused across in-process retry attempts.</param>
    /// <param name="next">The delegate that invokes the next behavior or the subscriber; may be invoked at most once.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The classified attempt <see cref="Result"/>.</returns>
    /// <remarks>Returning without invoking <paramref name="next"/> short-circuits the attempt; see <see cref="SubscriberBehaviorResult"/>. The <see cref="Result"/> returned by <paramref name="next"/> is already
    /// classified. An exception thrown by the behavior itself, or an unclassified failure it returns, is treated as <see cref="ErrorHandling.None"/>.</remarks>
    Task<Result> InvokeAsync(EventSubscriberArgs args, SubscriberBehaviorDelegate next, CancellationToken cancellationToken);

    /// <summary>
    /// Observes the settlement actually applied to the delivery; invoked once per delivery, after settlement, and only where the behavior participated.
    /// </summary>
    /// <param name="args">The per-delivery <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="disposition">The <see cref="SubscriberDisposition"/>.</param>
    /// <param name="cancellationToken">The receive <see cref="CancellationToken"/>; this may already be cancelled (e.g. host shutdown).</param>
    /// <returns>The <see cref="Task"/>.</returns>
    /// <remarks>Observe-only: the settlement cannot be changed. Exceptions are logged and suppressed. Defaults to no operation.</remarks>
    Task OnDispositionAsync(EventSubscriberArgs args, SubscriberDisposition disposition, CancellationToken cancellationToken) => Task.CompletedTask;
}
