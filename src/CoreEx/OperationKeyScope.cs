namespace CoreEx;

/// <summary>
/// Defines whether an operation key is scoped to a tenant or to its domain.
/// </summary>
public enum OperationKeyScope
{
    /// <summary>
    /// The operation is shared across tenants within its domain.
    /// </summary>
    DomainWide = 1,

    /// <summary>
    /// The operation belongs to one tenant within its domain.
    /// </summary>
    Tenant = 2
}
