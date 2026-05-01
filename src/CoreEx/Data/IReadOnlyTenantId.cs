namespace CoreEx.Data;

/// <summary>
/// Enables a read-only <see cref="TenantId"/>.
/// </summary>
public interface IReadOnlyTenantId
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    string? TenantId { get; }
}