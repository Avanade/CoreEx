namespace CoreEx.AspNetCore.Test.Unit;

internal sealed class OperationSnapshotStore : IOperationStore
{
    public OperationState State { get; set; } = new()
    {
        DomainName = "Test",
        OperationName = "person-submit",
        InstanceId = "private-submission-key",
        OperationId = Guid.NewGuid(),
        Status = CoreEx.Operations.OperationStatus.Accepted,
        InitiatedBy = "owner",
        InactivityTimeout = TimeSpan.FromDays(1),
        TerminalRetention = TimeSpan.FromDays(1),
        CreatedOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
        UpdatedOn = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
        AbandonAfter = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero),
        ETag = "constant-xmin",
        Data = """{"private":"extension-data"}""",
        ClaimOwner = "private-worker"
    };

    public Func<OperationState, OperationState>? OnCancel { get; set; }

    public Task<OperationState?> GetByIdAsync(Guid operationId, CancellationToken cancellationToken = default)
        => Task.FromResult<OperationState?>(State.OperationId == operationId ? State : null);

    public Task<OperationState?> RequestCancelAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        State = OnCancel?.Invoke(State) ?? State with { CancelRequestedOn = State.UpdatedOn, Status = CoreEx.Operations.OperationStatus.Cancelled };
        return Task.FromResult<OperationState?>(State);
    }

    public Task<OperationState?> GetByKeyAsync(string operationName, CoreEx.Entities.CompositeKey key, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<OperationCreateResult> CreateAsync(string operationName, CoreEx.Entities.CompositeKey key, string? initiatedBy = null, string? data = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<OperationCreateResult> CreateAsync(string operationName, CoreEx.Entities.CompositeKey key, string? initiatedBy, string? data, OperationCreateOptions options, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<OperationClaimResult> ClaimAsync(Guid operationId, string owner, TimeSpan duration, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<OperationClaim?> RenewAsync(OperationClaim claim, TimeSpan duration, string? progress = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<bool> ReleaseAsync(OperationClaim claim, string? errorCode = null, string? errorMessage = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<OperationState?> CompleteAsync(OperationClaim claim, OperationCompletion completion, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<OperationCleanupResult> CleanupAsync(int maxCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
