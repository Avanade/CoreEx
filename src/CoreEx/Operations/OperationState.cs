namespace CoreEx.Operations;

/// <summary>
/// Represents an immutable snapshot of a persisted tracked operation.
/// </summary>
/// <remarks>All timestamps are observed by the store (database time for relational stores), not <see cref="Runtime.UtcNow"/>. Result payloads are never stored; <see cref="ResultLocation"/> links to an application-owned resource.</remarks>
public sealed record OperationState
{
    /// <summary>
    /// Gets the stable domain name sourced from <see cref="IHostSettings.DomainName"/>.
    /// </summary>
    public required string DomainName { get; init; }

    /// <summary>
    /// Gets the tenant identifier for tenant-scoped work; otherwise, <see langword="null"/>.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets the stable application-defined operation name.
    /// </summary>
    public required string OperationName { get; init; }

    /// <summary>
    /// Gets the instance identifier formatted using <see cref="CompositeKey.ToCanonicalString"/>.
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// Gets the store-generated unique operation identifier used by status links.
    /// </summary>
    public required Guid OperationId { get; init; }

    /// <summary>
    /// Gets the <see cref="OperationStatus"/>.
    /// </summary>
    public required OperationStatus Status { get; init; }

    /// <summary>
    /// Gets the stable identifier of the initiating principal used for default authorization.
    /// </summary>
    public string? InitiatorId { get; init; }

    /// <summary>
    /// Gets the execution attempt (fencing token); incremented by every successful claim.
    /// </summary>
    public int Attempt { get; init; }

    /// <summary>
    /// Gets the owner token of the current execution claim, where claimed.
    /// </summary>
    public string? ClaimOwner { get; init; }

    /// <summary>
    /// Gets the store time at which the current execution claim expires, where claimed.
    /// </summary>
    public DateTimeOffset? ClaimExpiresOn { get; init; }

    /// <summary>
    /// Gets the store time at which cancellation was first requested while running.
    /// </summary>
    public DateTimeOffset? CancelRequestedOn { get; init; }

    /// <summary>
    /// Gets the latest user-safe progress text.
    /// </summary>
    public string? Progress { get; init; }

    /// <summary>
    /// Gets the user-safe code of the most recent error: the terminal error where <see cref="OperationStatus.Failed"/>, or the last retryable error recorded on release.
    /// </summary>
    /// <remarks>Retained while a subsequent attempt is <see cref="OperationStatus.Running"/> and on <see cref="OperationStatus.Expired"/>; cleared on <see cref="OperationStatus.Succeeded"/> or <see cref="OperationStatus.Cancelled"/> completion.</remarks>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the user-safe message of the most recent error (see <see cref="ErrorCode"/>).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the link to the application-owned result resource.
    /// </summary>
    public string? ResultLocation { get; init; }

    /// <summary>
    /// Gets the opaque application-defined JSON extension data; never queried by the store.
    /// </summary>
    public string? Data { get; init; }

    /// <summary>
    /// Gets the store time at which the operation was accepted.
    /// </summary>
    public required DateTimeOffset CreatedOn { get; init; }

    /// <summary>
    /// Gets the store time of the most recent persisted mutation.
    /// </summary>
    public required DateTimeOffset UpdatedOn { get; init; }

    /// <summary>
    /// Gets the store time of the first successful claim.
    /// </summary>
    public DateTimeOffset? StartedOn { get; init; }

    /// <summary>
    /// Gets the store time at which the operation finished.
    /// </summary>
    public DateTimeOffset? CompletedOn { get; init; }

    /// <summary>
    /// Gets the store time after which an unfinished operation with no further activity is abandoned and expired by cleanup; <see langword="null"/> once finished.
    /// </summary>
    /// <remarks>Set from <see cref="OperationStoreOptions.InactivityTimeout"/> on create and release, and from the claim expiry plus that timeout on claim and renew, so a held claim never expires.</remarks>
    public DateTimeOffset? AbandonAfter { get; init; }

    /// <summary>
    /// Gets the earliest store time at which a finished operation may be purged; <see langword="null"/> while active.
    /// </summary>
    public DateTimeOffset? PurgeAfter { get; init; }

    /// <summary>
    /// Gets the opaque store concurrency token that changes on every persisted mutation and on recreation.
    /// </summary>
    /// <remarks>This is not an HTTP representation ETag.</remarks>
    public required string ETag { get; init; }

    /// <summary>
    /// Indicates whether the operation has finished (<see cref="OperationStatus.Succeeded"/>, <see cref="OperationStatus.Failed"/>, <see cref="OperationStatus.Cancelled"/> or <see cref="OperationStatus.Expired"/>).
    /// </summary>
    [JsonIgnore]
    public bool IsFinished => Status is OperationStatus.Succeeded or OperationStatus.Failed or OperationStatus.Cancelled or OperationStatus.Expired;

    /// <summary>
    /// Creates an <see cref="OperationClaim"/> from this state.
    /// </summary>
    /// <returns>The held claim represented by this state.</returns>
    /// <exception cref="InvalidOperationException">The state does not contain a claim owner or expiry.</exception>
    internal OperationClaim ToClaim()
    {
        if (ClaimOwner is null || ClaimExpiresOn is null)
            throw new InvalidOperationException("The operation state does not contain a held claim.");

        return new(OperationId, ClaimOwner, Attempt, ClaimExpiresOn.Value, CancelRequestedOn.HasValue);
    }
}
