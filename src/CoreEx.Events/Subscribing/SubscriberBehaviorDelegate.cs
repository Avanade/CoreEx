namespace CoreEx.Events.Subscribing;

/// <summary>
/// Represents the next step in the <see cref="ISubscriberBehavior"/> pipeline.
/// </summary>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <returns>The classified <see cref="Result"/>.</returns>
public delegate Task<Result> SubscriberBehaviorDelegate(CancellationToken cancellationToken);
