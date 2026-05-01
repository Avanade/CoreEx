namespace CoreEx.Data;

/// <summary>
/// Enables a mutable <see cref="TenantId"/>.
/// </summary>
public interface ITenantId : IReadOnlyTenantId
{
    /// <inheritdoc/>
    string? IReadOnlyTenantId.TenantId => TenantId;

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    new string? TenantId { get; set; }
}