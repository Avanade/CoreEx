namespace CoreEx.Cosmos;

/// <summary>
/// Provides options for the <see cref="ICosmosDb"/> <see cref="Container"/>.
/// </summary>
public class CosmosDbContainerOptions
{
    private readonly List<Action<CosmosDbOutboxEvent, JsonSerializerOptions?>> _outboxEventUpdaters = [];

    /// <summary>
    /// Adds a <see cref="CosmosDbOutboxEvent"/> updater action to be invoked when an outbox event is being created.
    /// </summary>
    /// <param name="outboxEventUpdater">The action to update the outbox event.</param>
    /// <returns>The <see cref="CosmosDbContainerOptions"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This can be used to customize the outbox events when they are created; for example, setting an additional property to avoid a duplicate (see <see cref="CosmosDbOutboxEvent.ExtensionData"/>).
    /// Multiple updaters are supported and are invoked in the order added.</remarks>
    public CosmosDbContainerOptions WithOutboxEventUpdater(Action<CosmosDbOutboxEvent> outboxEventUpdater)
    {
        outboxEventUpdater.ThrowIfNull();
        _outboxEventUpdaters.Add((e, _) => outboxEventUpdater(e));
        return this;
    }

    /// <summary>
    /// Adds a <see cref="CosmosDbOutboxEvent"/> updater action to be invoked when an outbox event is being created, which is also provided the <see cref="JsonSerializerOptions"/> used by the <see cref="CosmosClient"/>.
    /// </summary>
    /// <param name="outboxEventUpdater">The action to update the outbox event; the <see cref="JsonSerializerOptions"/> will be <see langword="null"/> where the <see cref="CosmosClient"/> does not specify any.</param>
    /// <returns>The <see cref="CosmosDbContainerOptions"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Required where the property name emitted must honor the serializer's <see cref="JsonSerializerOptions.PropertyNamingPolicy"/>, as <see cref="CosmosDbOutboxEvent.ExtensionData"/> keys are written as-is.</remarks>
    public CosmosDbContainerOptions WithOutboxEventUpdater(Action<CosmosDbOutboxEvent, JsonSerializerOptions?> outboxEventUpdater)
    {
        _outboxEventUpdaters.Add(outboxEventUpdater.ThrowIfNull());
        return this;
    }

    /// <summary>
    /// Adds a <see cref="CosmosDbOutboxEvent"/> updater action to be invoked when an outbox event is being created for a reference data item to uniquely set the
    /// <see cref="RefData.Abstractions.IReferenceData.Code"/> as an additional property (see <see cref="CosmosDbOutboxEvent.ExtensionData"/>) to avoid a duplicate.
    /// </summary>
    /// <returns>The <see cref="CosmosDbContainerOptions"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Outbox events share the container (and partition) with the business documents, so a unique key policy (e.g. <c>/typeDiscriminator</c> and <c>/code</c>) would otherwise treat every outbox event as the same
    /// (<see langword="null"/>, <see langword="null"/>) key and reject all but the first. The property name honors the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> of the <see cref="CosmosClient"/> (falling back
    /// to <see cref="Json.JsonDefaults.SerializerOptions"/>), so that it matches that of the business documents.</remarks>
    public CosmosDbContainerOptions WithReferenceDataOutboxEvent()
        => WithOutboxEventUpdater((e, jso) =>
        {
            var name = (jso ?? Json.JsonDefaults.SerializerOptions).PropertyNamingPolicy?.ConvertName(nameof(RefData.Abstractions.IReferenceData.Code)) ?? nameof(RefData.Abstractions.IReferenceData.Code);

            e.ExtensionData ??= [];
            e.ExtensionData.TryAdd(name, e.Id);
        });

    /// <summary>
    /// Applies the <see cref="CosmosDbOutboxEvent"/> updater actions to the specified <paramref name="outboxEvent"/>.
    /// </summary>
    /// <param name="outboxEvent">The outbox event to apply the updater actions to.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used by the <see cref="CosmosClient"/>, where specified.</param>
    public void ApplyOutboxEventUpdater(CosmosDbOutboxEvent outboxEvent, JsonSerializerOptions? serializerOptions = null)
    {
        outboxEvent.ThrowIfNull();
        foreach (var updater in _outboxEventUpdaters)
            updater(outboxEvent, serializerOptions);
    }
}