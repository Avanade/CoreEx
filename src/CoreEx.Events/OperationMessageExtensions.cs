namespace CoreEx.Events;

/// <summary>
/// Adds explicit asynchronous-operation identity to an initiating message.
/// </summary>
public static class OperationMessageExtensions
{
    /// <summary>
    /// Attaches the complete operation identity and first-start deadline to the message.
    /// </summary>
    /// <param name="eventData">The message.</param>
    /// <param name="operationKey">The complete explicit operation identity.</param>
    /// <param name="startBy">The provider-calculated first-start deadline.</param>
    /// <returns>The message.</returns>
    public static EventData WithOperation(this EventData eventData, OperationKey operationKey, DateTimeOffset startBy)
    {
        eventData.ThrowIfNull();
        operationKey.ThrowIfNull();

        eventData
            .SetAttribute("operationid", operationKey.InstanceId)
            .SetAttribute("operationdomain", operationKey.DomainName)
            .SetAttribute("operationscope", operationKey.Scope == OperationKeyScope.Tenant ? "tenant" : "domain")
            .SetAttribute("operationname", operationKey.OperationName)
            .SetAttribute("operationstartby", startBy.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        if (operationKey.TenantId is not null)
            eventData.SetAttribute("operationtenantid", operationKey.TenantId);

        return eventData;
    }
}
