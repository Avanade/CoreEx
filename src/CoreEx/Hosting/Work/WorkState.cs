namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the status and metadata of a long-running <see cref="WorkOrchestrator"/>-tracked work instance.
/// </summary>
public class WorkState : IIdentifier<string?>, IETag
{
    /// <summary>
    /// Gets or sets whether this work is scoped to a tenant or its domain.
    /// </summary>
    public required OperationKeyScope Scope { get; init; }

    /// <summary>
    /// Gets or sets the stable domain name for this work.
    /// </summary>
    public required string DomainName { get; init; }

    /// <summary>
    /// Gets or sets the tenant identifier for tenant-scoped work; otherwise, <see langword="null"/>.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the stable application-defined operation name.
    /// </summary>
    public required string OperationName { get; init; }

    /// <summary>
    /// Gets the complete operation identity reconstructed from this work state's identity fields.
    /// </summary>
    /// <remarks>This computed value is not serialized; providers create storage keys from the state fields.</remarks>
    [JsonIgnore]
    public OperationKey OperationKey => new(Scope, DomainName, TenantId, OperationName, Id.ThrowIfNullOrWhiteSpace(nameof(Id)));

    /// <summary>
    /// Gets or sets the operation instance identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> type name.
    /// </summary>
    /// <remarks>Enables separation between one or more <see cref="WorkState"/> types.</remarks>
    [JsonPropertyName("type")]
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the related entity key where applicable.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the trace parent (defaults to see <see cref="System.Diagnostics.Activity.Id"/>).
    /// </summary>
    public string? TraceParent { get; set; }

    /// <summary>
    /// Gets or sets the trace state (defaults to see <see cref="System.Diagnostics.Activity.TraceStateString"/>).
    /// </summary>
    public string? TraceState { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus"/>.
    /// </summary>
    public WorkStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the owning <see cref="AuthenticationUser"/>.
    /// </summary>
    /// <remarks>This provides a basic authorization-style opportunity by verifying <i>only</i> the initiating user has ongoing access.</remarks>
    public AuthenticationUser? User { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Created"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonPropertyName("created")]
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the provider timestamp of the most recent persisted state change.
    /// </summary>
    /// <remarks>May be absent on states written before this property was introduced.</remarks>
    [JsonPropertyName("updated")]
    public DateTimeOffset? UpdatedOn { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkOrchestrator"/> expiry <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <remarks>Where the work has not <see cref="WorkStatus.Finished"/> by the expiry it will be automatically <see cref="WorkStatus.Expired"/>.</remarks>
    [JsonPropertyName("expiry")]
    public DateTimeOffset ExpiryOn { get; set; }

    /// <summary>
    /// Gets or sets the latest time a created work item may start.
    /// </summary>
    [JsonPropertyName("startBy")]
    public DateTimeOffset? StartBy { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Started"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonPropertyName("started")]
    public DateTimeOffset? StartedOn { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Indeterminate"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonPropertyName("indeterminate")]
    public DateTimeOffset? IndeterminateOn { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkStatus.Finished"/> <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonPropertyName("finished")]
    public DateTimeOffset? FinishedOn { get; set; }

    /// <summary>
    /// Gets or sets the current worker lease owner token.
    /// </summary>
    [JsonPropertyName("leaseOwner")]
    public string? LeaseOwner { get; set; }

    /// <summary>
    /// Gets or sets the current worker lease expiry.
    /// </summary>
    [JsonPropertyName("leaseExpires")]
    public DateTimeOffset? LeaseExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets the current lease epoch.
    /// </summary>
    public int Attempt { get; set; }

    /// <summary>
    /// Gets or sets whether cancellation was requested while the work was in progress.
    /// </summary>
    public bool CancelRequested { get; set; }

    /// <summary>
    /// Gets or sets the user-safe work error.
    /// </summary>
    public WorkError? Error { get; set; }

    /// <summary>
    /// Gets or sets the latest progress report.
    /// </summary>
    public WorkProgress? Progress { get; set; }

    /// <summary>
    /// Gets or sets the app-relative URI of the domain resource produced by this work.
    /// </summary>
    public string? ResourceLocation { get; set; }

    /// <summary>
    /// Gets or sets the revision used to resolve ambiguous writes.
    /// </summary>
    [JsonPropertyName("rev")]
    public string? Revision { get; set; }

    /// <inheritdoc/>
    /// <remarks>Maintained by the <see cref="IWorkProvider"/> to enable optimistic concurrency; any persisted value is ignored.</remarks>
    public string? ETag { get; set; }
}
