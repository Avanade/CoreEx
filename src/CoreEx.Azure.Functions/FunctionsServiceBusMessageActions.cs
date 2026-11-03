namespace CoreEx.Azure.Functions;

/// <summary>
/// Adapts Functions message settlement to CoreEx actions and settlement metrics.
/// </summary>
/// <param name="message">The received message.</param>
/// <param name="actions">The Functions settlement actions.</param>
/// <param name="entityPath">The queue or topic/subscription path used for metrics, optionally containing <c>%SettingName%</c> references.</param>
public sealed class FunctionsServiceBusMessageActions(ServiceBusReceivedMessage message, ServiceBusMessageActions actions, string entityPath) : ServiceBusMessageActionsBase
{
    private readonly ServiceBusReceivedMessage _message = message.ThrowIfNull();
    private readonly ServiceBusMessageActions _actions = actions.ThrowIfNull();

    /// <inheritdoc/>
    public override string EntityPath { get; } = ResolveEntityPath(entityPath);

    /// <summary>
    /// Resolves setting references using the ambient worker configuration without recursively expanding setting values.
    /// </summary>
    private static string ResolveEntityPath(string entityPath)
    {
        entityPath.ThrowIfNullOrEmpty();
        if (!entityPath.Contains('%'))
            return entityPath;

        var path = new StringBuilder();
        IConfiguration? configuration = null;
        var offset = 0;
        while (offset < entityPath.Length)
        {
            var start = entityPath.IndexOf('%', offset);
            if (start < 0)
            {
                path.Append(entityPath, offset, entityPath.Length - offset);
                break;
            }

            path.Append(entityPath, offset, start - offset);
            var end = entityPath.IndexOf('%', start + 1);
            if (end < 0 || string.IsNullOrWhiteSpace(entityPath[(start + 1)..end]))
                throw new ArgumentException("The entity path contains an unmatched '%' or an empty setting reference.", nameof(entityPath));

            var key = entityPath[(start + 1)..end];
            configuration ??= ExecutionContext.GetRequiredService<IConfiguration>();
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"The entity path setting '{key}' is missing or empty.");

            path.Append(value);
            offset = end + 1;
        }

        return path.ToString();
    }

    /// <inheritdoc/>
    public override AmqpAnnotatedMessage AmqpMessage => _message.GetRawAmqpMessage();

    /// <inheritdoc/>
    protected override Task OnCompletedMessageAsync(CancellationToken cancellationToken) => _actions.CompleteMessageAsync(_message, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnAbandonedMessageAsync(Exception exception, CancellationToken cancellationToken)
        => _actions.AbandonMessageAsync(_message, new Dictionary<string, object> { { AbandonReasonName, FormatText(exception.Message, NoneReasonText).ThrowIfNull() } }, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Full exceptions belong in logs, not broker-persisted dead-letter descriptions.</remarks>
    protected override Task OnDeadLetteredMessageAsync(Exception exception, CancellationToken cancellationToken)
        => _actions.DeadLetterMessageAsync(_message, deadLetterReason: FormatText(exception.Message, NoneReasonText), cancellationToken: cancellationToken);
}
