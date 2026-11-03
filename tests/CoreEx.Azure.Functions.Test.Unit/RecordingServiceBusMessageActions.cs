namespace CoreEx.Azure.Functions.Test.Unit;

internal sealed class RecordingServiceBusMessageActions : ServiceBusMessageActions
{
    public List<string> Settlements { get; } = [];
    public ServiceBusReceivedMessage? Message { get; private set; }
    public CancellationToken Token { get; private set; }
    public IDictionary<string, object>? Properties { get; private set; }
    public string? DeadLetterReason { get; private set; }
    public string? DeadLetterDescription { get; private set; }
    public Exception? Failure { get; set; }

    public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        => Record("complete", message, cancellationToken);

    public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object>? propertiesToModify = default, CancellationToken cancellationToken = default)
    {
        Properties = propertiesToModify;
        return Record("abandon", message, cancellationToken);
    }

    public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, Dictionary<string, object>? propertiesToModify = default,
        string? deadLetterReason = default, string? deadLetterErrorDescription = default, CancellationToken cancellationToken = default)
    {
        Properties = propertiesToModify;
        DeadLetterReason = deadLetterReason;
        DeadLetterDescription = deadLetterErrorDescription;
        return Record("dead-letter", message, cancellationToken);
    }

    private Task Record(string settlement, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        Settlements.Add(settlement);
        Message = message;
        Token = cancellationToken;
        return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
}
