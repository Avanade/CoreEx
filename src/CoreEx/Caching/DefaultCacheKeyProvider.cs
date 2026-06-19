namespace CoreEx.Caching;

/// <summary>
/// Provides the default implementation of <see cref="ICacheKeyProvider"/>.
/// </summary>
/// <param name="hostSettings">The optional <see cref="IHostSettings"/>.</param>
/// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
/// <remarks>This implementation uses <see cref="IHostSettings.DomainName"/> and <see cref="ExecutionContext.TenantId"/> to provide namespacing/partitioning of cache keys where applicable.</remarks>
public sealed class DefaultCacheKeyProvider(IHostSettings? hostSettings = null, ExecutionContext? executionContext = null) : ICacheKeyProvider
{
    private readonly IHostSettings? _hostSettings = hostSettings;
    private readonly ExecutionContext? _executionContext = executionContext;

    /// <inheritdoc/>
    /// <remarks>This implementation uses the following format: '<c>{DomainName}:{TenantId}:{Key}</c>' where <c>DomainName</c> is obtained from <see cref="IHostSettings.DomainName"/> and <c>TenantId</c> is obtained from <see cref="ExecutionContext.TenantId"/>.
    /// Where either value is not available, it is simply omitted along with the associated colon ('<c>:</c>').</remarks>
    public string GetFullyQualifiedCacheKey(string key) => _hostSettings is null
        ? _executionContext?.TenantId is null ? key : $"{_executionContext.TenantId}:{key}"
        : _executionContext?.TenantId is null ? $"{_hostSettings.DomainName}:{key}" : $"{_hostSettings.DomainName}:{_executionContext.TenantId}:{key}";

    /// <inheritdoc/>
    /// <remarks>This implementation uses the <see cref="IEntityKey"/> <see cref="Type"/> <see cref="MemberInfo.Name"/> of <typeparamref name="T"/> as a prefix, then a '<c>:</c>', followed by the <see cref="CompositeKey.ToString()"/>. For example, 
    /// the '<c>CoreEx.Testing.Product</c>' class with a key or 'Abc' would result in '<c>Product:Abc</c>'.
    /// </remarks>
    public string GetEntityCacheKey<T>(CompositeKey key) where T : IEntityKey => $"{typeof(T).Name}:{key.ToString() ?? string.Empty}";
}