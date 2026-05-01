namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the base capabilities for a subscribed event receiver.
/// </summary>
/// <remarks>See <see cref="SubscribedManager"/> that manages the subscribed instance lifetime and execution management.</remarks>
public abstract partial class SubscribedBase
{
    /// <summary>
    /// Gets or sets the optional customizable <see cref="Subscribing.ErrorHandler"/> configuration that is specific to the subscribed event/message receiving.
    /// </summary>
    /// <remarks>Where not specified the owning <see cref="EventSubscriberBase"/> will handle as a fallback.</remarks>
    public ErrorHandler? ErrorHandler { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> where the <see cref="EventData.Data"/> is not considered <i>valid</i>.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.
    /// <para>When specified overrides the parent <see cref="EventSubscriberBase.InvalidDataHandling"/>.</para></remarks>
    public ErrorHandling? InvalidDataHandling { get; set; }

    /// <summary>
    /// Indicates whether the <see cref="EventData"/> requires an <b>inbox</b> check before processing.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.
    /// <para>When specified overrides the parent <see cref="SubscribedManager.RequiresInboxCheck"/>.</para></remarks>
    public bool? RequiresInboxCheck { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> for when the <see cref="EventData"/> fails the <see cref="IEventSubscriberInbox.InboxCheckAsync"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.
    /// <para>When specified overrides the parent <see cref="SubscribedManager.InboxFailureHandling"/>.</para></remarks>
    public ErrorHandling? InboxFailureHandling { get; set; }

    /// <summary>
    /// Gets or sets the optional <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.
    /// <para>When specified overrides the <see cref="EventSubscriberBase.JsonSerializerOptions"/>.</para></remarks>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Receives and processes the <paramref name="event"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    public Task<Result> ReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default) => OnReceiveAsync(@event, args, cancellationToken);

    /// <summary>
    /// Receives and processes the <paramref name="event"/>.
    /// </summary>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The optional <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>Any exception thrown will be automatically converted to a <see cref="Result.Fail(Exception)"/>; however, it is considered more performant if an errant <see cref="Result"/> is returned natively.</remarks>
    protected abstract Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes the <see cref="EventData.Data"/> value to the specified <typeparamref name="TValue"/> type.
    /// </summary>
    /// <typeparam name="TValue">The <see cref="EventData.Data"/> value <see cref="Type"/>.</typeparam>
    /// <param name="event">The <see cref="EventData"/>.</param>
    /// <param name="args">The <see cref="EventSubscriberArgs"/>.</param>
    /// <param name="valueIsRequired">Indicates whether the <see cref="EventData.Data"/> value is required.</param>
    /// <returns>The deserialized value within a <see cref="Result{T}.Value"/>.</returns>
    /// <remarks>Where an invalid data error (see <see cref="InvalidDataHandling"/>) occurs a <see cref="EventSubscriberReceiveException"/> will be thrown.</remarks>
    protected Result<TValue> DeserializeValue<TValue>(EventData @event, EventSubscriberArgs args, bool valueIsRequired)
        => args.Owner!.DeserializeValue<TValue>(@event, valueIsRequired, InvalidDataHandling, JsonSerializerOptions);
}