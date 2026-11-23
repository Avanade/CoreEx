namespace CoreEx.Redis;

/// <summary>
/// Creates discoverable Redis keys for complete operation identities.
/// </summary>
internal static class RedisKeyFormatter
{
    private static readonly UTF8Encoding _strictUtf8 = new(false, true);

    /// <summary>
    /// Appends escaped identity segments to the purpose prefix.
    /// </summary>
    /// <remarks>The <c>tenant</c> segment is omitted when <paramref name="tenantId"/> is <see langword="null"/> or empty (domain-wide).</remarks>
    internal static RedisKey Create(string prefix, string domainName, string? tenantId, string operationName, string instanceId)
    {
        ValidateUnicode(prefix, nameof(prefix));
        var separator = prefix.EndsWith(':') ? string.Empty : ":";
        var domain = Escape(domainName.ThrowIfNullOrWhiteSpace(), nameof(domainName));
        var tenant = string.IsNullOrEmpty(tenantId) ? string.Empty : $":tenant:{Escape(tenantId, nameof(tenantId))}";
        var operation = Escape(operationName.ThrowIfNullOrWhiteSpace(), nameof(operationName));
        var instance = Escape(instanceId.ThrowIfNullOrWhiteSpace(), nameof(instanceId));
        return $"{prefix}{separator}domain:{domain}{tenant}:operation:{operation}:instance:{instance}";
    }

    /// <summary>
    /// Gets the ambient <see cref="ExecutionContext.TenantId"/>, normalizing an empty value to <see langword="null"/>.
    /// </summary>
    internal static string? GetTenantId() => ExecutionContext.TryGetCurrent(out var ec) && !string.IsNullOrEmpty(ec.TenantId) ? ec.TenantId : null;

    /// <summary>
    /// Rejects malformed Unicode that would otherwise be replaced during UTF-8 encoding.
    /// </summary>
    internal static void ValidateUnicode(string value, string parameterName)
    {
        try
        {
            _strictUtf8.GetByteCount(value);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ArgumentException("Redis key values must contain valid Unicode.", parameterName, exception);
        }
    }

    /// <summary>
    /// Escapes an identity segment without allowing replacement-character collisions.
    /// </summary>
    private static string Escape(string value, string parameterName)
    {
        ValidateUnicode(value, parameterName);
        return Uri.EscapeDataString(value);
    }
}
