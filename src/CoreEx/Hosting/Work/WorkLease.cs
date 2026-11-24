namespace CoreEx.Hosting.Work;

/// <summary>
/// Identifies the current worker lease for a work item.
/// </summary>
/// <param name="DomainName">The stable domain name.</param>
/// <param name="TenantId">The tenant identifier for tenant-scoped work; otherwise, <see langword="null"/>.</param>
/// <param name="OperationName">The stable application-defined operation name.</param>
/// <param name="Id">The operation instance identifier.</param>
/// <param name="Owner">The opaque lease owner token.</param>
/// <param name="Epoch">The monotonically increasing lease epoch.</param>
/// <param name="ExpiresOn">The lease expiry observed when the lease was issued.</param>
public sealed record WorkLease(string DomainName, string? TenantId, string OperationName, string Id, string Owner, int Epoch, DateTimeOffset ExpiresOn);
