namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the base capabilities for a subscribed event receiver with a specified <typeparamref name="TValue"/> <see cref="Type"/> automatically handling deserialization.
/// </summary>
/// <typeparam name="TValue">The <see cref="EventData.Data"/> value <see cref="Type"/>.</typeparam>
public abstract class SubscribedBase<TValue> : SubscribedBase
{
    /// <summary>
    /// Indicates whether the <see cref="EventData.Data"/> value is required.
    /// </summary>
    public virtual bool ValueIsRequired { get; } = true;

    /// <summary>
    /// Gets or sets the optional <see cref="IValidator{T}"/> to use when validating the deserialized <see cref="EventData.Data"/> value.
    /// </summary>
    /// <remarks>This is invoked automatically prior to the <see cref="OnReceiveAsync(TValue, EventData, EventSubscriberArgs, CancellationToken)"/>.</remarks>
    public virtual IValidator<TValue>? ValueValidator { get; }

    /// <inheritdoc/>
    protected override sealed Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => DeserializeValue<TValue>(@event, args, ValueIsRequired)
            .WhenAsAsync(v => ValueValidator is not null, async v =>
            {
                var vr = await ValueValidator!.ValidateAsync(v, cancellationToken).ConfigureAwait(false);
                Result<TValue> r = vr.HasErrors ? vr.ToResult() : Result.Ok(v);
                return r;
            })
            .ThenAsAsync(async v => await OnReceiveAsync(v, @event, args, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Receives and processes the <paramref name="event"/>.
    /// </summary>
    /// <param name="value">The deserialized <paramref name="event"/> (<see cref="EventData.Data"/>) value.</param>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task<Result> OnReceiveAsync(TValue value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default);
}