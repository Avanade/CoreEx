namespace CoreEx.Operations;

/// <summary>
/// Persists tracked operations using atomic, conditional lifecycle commands evaluated against store-observed time.
/// An operation name and <see cref="CompositeKey"/>, qualified by the host <see cref="IHostSettings.DomainName"/> and the ambient <see cref="ExecutionContext.TenantId"/> (where present), identify logical work; an <see cref="OperationState.OperationId"/> identifies one persisted incarnation of that work.
/// Use <see cref="GetByKeyAsync"/> to find work by application identity and <see cref="GetByIdAsync"/> for the handle returned to callers (for example, in a 202 status location) and carried in worker messages.
/// </summary>
/// <remarks>The store is independent of HTTP idempotency and coordination: work may be started by an API, subscriber, timer or any other process. <see cref="CreateAsync(string, CompositeKey, string?, string?, CancellationToken)"/> is itself idempotent on that identity, so a caller retrying acceptance (for example, after an unknown commit outcome) receives the existing operation rather than a duplicate.
/// Recreating work under the same key after purge assigns a new operation identifier, so stale handles and deliveries cannot target the replacement. Neither lookup grants authorization; callers must enforce domain, tenant and initiator access.
/// Every command is a single atomic conditional write; there is no general read-modify-write update. For relational stores:
/// <list type="bullet">
/// <item><see cref="CreateAsync(string, CompositeKey, string?, string?, CancellationToken)"/> and <see cref="CompleteAsync"/> execute on the ambient database connection and transaction, so they commit or roll back with the business unit of work and its transactional outbox.</item>
/// <item><see cref="ClaimAsync"/>, <see cref="RenewAsync"/> and <see cref="ReleaseAsync"/> must execute in short independent scopes, never inside a long-running business transaction.</item>
/// <item>Claim expiry, completion fencing and purge eligibility are evaluated using database time.</item>
/// </list>
/// A <see langword="null"/>/<see langword="false"/> result from a fenced command means the fence was lost; the caller must abort (roll back) its unit of work rather than treat it as success.</remarks>
public interface IOperationStore
{
    /// <summary>
    /// Gets the operation by its <paramref name="operationId"/>.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationState"/> where found; otherwise, <see langword="null"/>.</returns>
    Task<OperationState?> GetByIdAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the operation by its complete identity.
    /// </summary>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="key">The operation instance key; its canonical string must not be empty or whitespace. Qualified by the host domain name and ambient tenant.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationState"/> where found; otherwise, <see langword="null"/>.</returns>
    Task<OperationState?> GetByKeyAsync(string operationName, CompositeKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically creates an <see cref="OperationStatus.Accepted"/> operation where its identity does not already exist.
    /// </summary>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="key">The operation instance key; its canonical string must not be empty or whitespace. Qualified by the host domain name and ambient tenant, and compared ordinally (including case and trailing spaces) using <see cref="CompositeKey.ToCanonicalString"/>.</param>
    /// <param name="initiatedBy">The initiating <see cref="AuthenticationUser.UserName"/>, following the <see cref="ChangeLog.CreatedBy"/> convention. Callers must establish a trusted username before using it for ownership authorization.</param>
    /// <param name="data">The optional opaque JSON extension data.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationCreateResult"/>; an existing operation (including a finished one not yet purged) is returned rather than replaced.</returns>
    Task<OperationCreateResult> CreateAsync(string operationName, CompositeKey key, string? initiatedBy = null, string? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically creates or gets an operation with optional lifetime overrides.
    /// </summary>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="key">The operation instance key.</param>
    /// <param name="initiatedBy">The trusted initiating username.</param>
    /// <param name="data">The optional opaque extension data.</param>
    /// <param name="options">The optional lifetime overrides; retained for this incarnation and ignored for an existing operation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The created or existing operation.</returns>
    Task<OperationCreateResult> CreateAsync(string operationName, CompositeKey key, string? initiatedBy, string? data, OperationCreateOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims an <see cref="OperationStatus.Accepted"/> or <see cref="OperationStatus.Retrying"/> operation, or takes over a <see cref="OperationStatus.Running"/> operation whose claim has expired.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="owner">The unique owner token for this execution (for example, per delivery).</param>
    /// <param name="duration">The claim duration from store time.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationClaimResult"/>.</returns>
    Task<OperationClaimResult> ClaimAsync(Guid operationId, string owner, TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends a held, unexpired claim and optionally records progress.
    /// </summary>
    /// <param name="claim">The held <see cref="OperationClaim"/>.</param>
    /// <param name="duration">The new claim duration from store time.</param>
    /// <param name="progress">The optional user-safe progress text; <see langword="null"/> leaves it unchanged.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The renewed <see cref="OperationClaim"/>; otherwise, <see langword="null"/> where the claim was lost.</returns>
    Task<OperationClaim?> RenewAsync(OperationClaim claim, TimeSpan duration, string? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a held claim after a retryable failure (or graceful shutdown), transitioning the operation to <see cref="OperationStatus.Retrying"/> so a redelivery can claim it immediately.
    /// </summary>
    /// <param name="claim">The held <see cref="OperationClaim"/>.</param>
    /// <param name="errorCode">The optional user-safe code of the retryable error; replaces any previously recorded error.</param>
    /// <param name="errorMessage">The optional user-safe message of the retryable error; requires <paramref name="errorCode"/> and must never include sensitive data.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where released; otherwise, <see langword="false"/> where the claim was already lost.</returns>
    /// <remarks>Release only after the attempt's unit of work has rolled back. A failure that will not be retried is recorded with <see cref="CompleteAsync"/> and <see cref="OperationCompletion.Failed"/>.</remarks>
    Task<bool> ReleaseAsync(OperationClaim claim, string? errorCode = null, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically finishes the operation where the <paramref name="claim"/> is still held and unexpired.
    /// </summary>
    /// <param name="claim">The held <see cref="OperationClaim"/>.</param>
    /// <param name="completion">The <see cref="OperationCompletion"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The finished <see cref="OperationState"/>; otherwise, <see langword="null"/> where the fence was lost and the unit of work must be rolled back.</returns>
    Task<OperationState?> CompleteAsync(OperationClaim claim, OperationCompletion completion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cancellation: an <see cref="OperationStatus.Accepted"/> or <see cref="OperationStatus.Retrying"/> operation (no claim held) is cancelled immediately; a <see cref="OperationStatus.Running"/> operation is flagged for cooperative cancellation; a finished operation is unchanged.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="OperationState"/> where found; otherwise, <see langword="null"/>.</returns>
    Task<OperationState?> RequestCancelAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs one bounded cleanup pass: first expires up to <paramref name="maxCount"/> abandoned operations (unfinished and past <see cref="OperationState.AbandonAfter"/>) as <see cref="OperationStatus.Expired"/>, logging each as a warning; then deletes up to <paramref name="maxCount"/> finished operations past their <see cref="OperationState.PurgeAfter"/>.
    /// </summary>
    /// <param name="maxCount">The maximum number of records to expire, and separately to delete, in this pass.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="OperationCleanupResult"/>.</returns>
    /// <remarks>An expired operation is retained (and reportable) for its <see cref="OperationState.TerminalRetention"/> before it is purged; a redelivery that arrives afterwards observes <see cref="OperationClaimOutcome.Finished"/> and must not execute. Operations with a held claim are never expired, and active operations are never purged. Once purged, the same identity may be created again with a new operation identifier.</remarks>
    Task<OperationCleanupResult> CleanupAsync(int maxCount, CancellationToken cancellationToken = default);
}
