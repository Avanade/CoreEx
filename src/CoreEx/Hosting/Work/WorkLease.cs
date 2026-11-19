namespace CoreEx.Hosting.Work;

/// <summary>
/// Identifies the current worker lease for a work item.
/// </summary>
/// <param name="WorkId">The work identifier.</param>
/// <param name="Owner">The opaque lease owner token.</param>
/// <param name="Epoch">The monotonically increasing lease epoch.</param>
/// <param name="ExpiresOn">The lease expiry observed when the lease was issued.</param>
public sealed record WorkLease(string WorkId, string Owner, int Epoch, DateTimeOffset ExpiresOn);
