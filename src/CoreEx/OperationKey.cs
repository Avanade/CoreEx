namespace CoreEx;

/// <summary>
/// Identifies an operation using an explicit domain, scope, operation name and instance identifier.
/// </summary>
/// <remarks>Identity components are case-sensitive and are not normalized. Supply canonical domain and tenant identifiers. Providers are responsible for representing this identity in their own storage.</remarks>
public sealed record OperationKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationKey"/> class.
    /// </summary>
    /// <param name="scope">The explicit scope of the operation.</param>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="tenantId">The tenant identifier for tenant-scoped operations; must be null for domain-wide operations.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The identifier for this operation instance.</param>
    [JsonConstructor]
    public OperationKey(OperationKeyScope scope, string domainName, string? tenantId, string operationName, string instanceId)
    {
        if (!Enum.IsDefined(scope))
            throw new ArgumentOutOfRangeException(nameof(scope));

        Scope = scope;
        DomainName = domainName.ThrowIfNullOrWhiteSpace();
        OperationName = operationName.ThrowIfNullOrWhiteSpace();
        InstanceId = instanceId.ThrowIfNullOrWhiteSpace();

        if (scope == OperationKeyScope.Tenant)
            TenantId = tenantId.ThrowIfNullOrWhiteSpace();
        else if (tenantId is not null)
            throw new ArgumentException("A domain-wide operation key cannot include a tenant identifier.", nameof(tenantId));
    }

    /// <summary>
    /// Gets the explicit scope of the operation.
    /// </summary>
    public OperationKeyScope Scope { get; }

    /// <summary>
    /// Gets the stable domain name.
    /// </summary>
    public string DomainName { get; }

    /// <summary>
    /// Gets the tenant identifier for tenant-scoped operations; otherwise, <see langword="null"/>.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the stable application-defined operation name.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the identifier for this operation instance.
    /// </summary>
    public string InstanceId { get; }

    /// <summary>
    /// Creates a domain-wide operation key that is explicitly shared across tenants in the specified domain.
    /// </summary>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The identifier for this operation instance; a new identifier is generated when omitted.</param>
    /// <returns>The operation key.</returns>
    public static OperationKey ForDomain(string domainName, string operationName, string? instanceId = null)
        => new(OperationKeyScope.DomainWide, domainName, null, operationName, instanceId ?? Runtime.NewId());

    /// <summary>
    /// Creates a domain-wide operation key with a composite instance identifier.
    /// </summary>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The composite identifier for this operation instance.</param>
    /// <param name="formatter">The formatter used to produce the instance identifier. It must be deterministic and unambiguous.</param>
    /// <returns>The operation key.</returns>
    public static OperationKey ForDomain(string domainName, string operationName, CompositeKey instanceId, Func<CompositeKey, string> formatter)
        => ForDomain(domainName, operationName, instanceId.ToString(formatter));

    /// <summary>
    /// Creates a domain-wide operation key with a composite instance identifier using the collision-resistant formatter.
    /// </summary>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The composite identifier for this operation instance.</param>
    /// <returns>The operation key.</returns>
    public static OperationKey ForDomain(string domainName, string operationName, CompositeKey instanceId)
        => ForDomain(domainName, operationName, instanceId, CompositeKey.CollisionResistantStringFormatter);

    /// <summary>
    /// Creates a tenant-scoped operation key.
    /// </summary>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="tenantId">The required tenant identifier.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The identifier for this operation instance; a new identifier is generated when omitted.</param>
    /// <returns>The operation key.</returns>
    public static OperationKey ForTenant(string domainName, string tenantId, string operationName, string? instanceId = null)
        => new(OperationKeyScope.Tenant, domainName, tenantId, operationName, instanceId ?? Runtime.NewId());

    /// <summary>
    /// Creates a tenant-scoped operation key with a composite instance identifier.
    /// </summary>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="tenantId">The required tenant identifier.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The composite identifier for this operation instance.</param>
    /// <param name="formatter">The formatter used to produce the instance identifier. It must be deterministic and unambiguous.</param>
    /// <returns>The operation key.</returns>
    public static OperationKey ForTenant(string domainName, string tenantId, string operationName, CompositeKey instanceId, Func<CompositeKey, string> formatter)
        => ForTenant(domainName, tenantId, operationName, instanceId.ToString(formatter));

    /// <summary>
    /// Creates a tenant-scoped operation key with a composite instance identifier using the collision-resistant formatter.
    /// </summary>
    /// <param name="domainName">The stable domain name.</param>
    /// <param name="tenantId">The required tenant identifier.</param>
    /// <param name="operationName">The stable application-defined operation name.</param>
    /// <param name="instanceId">The composite identifier for this operation instance.</param>
    /// <returns>The operation key.</returns>
    public static OperationKey ForTenant(string domainName, string tenantId, string operationName, CompositeKey instanceId)
        => ForTenant(domainName, tenantId, operationName, instanceId, CompositeKey.CollisionResistantStringFormatter);
}
