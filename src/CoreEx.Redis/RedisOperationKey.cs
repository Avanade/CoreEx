namespace CoreEx.Redis;

/// <summary>
/// Creates discoverable Redis keys for explicit operation identities.
/// </summary>
internal static class RedisOperationKey
{
    private static readonly UTF8Encoding _strictUtf8 = new(false, true);

    /// <summary>
    /// Appends escaped identity segments to the purpose prefix.
    /// </summary>
    internal static RedisKey Create(string prefix, OperationKey operationKey)
    {
        operationKey.ThrowIfNull();
        ValidateUnicode(prefix, nameof(prefix));
        var separator = prefix.EndsWith(':') ? string.Empty : ":";
        var domain = Escape(operationKey.DomainName, nameof(operationKey.DomainName));
        var scope = operationKey.Scope == OperationKeyScope.Tenant ? "tenant" : "domain";
        var tenant = operationKey.TenantId is null ? "-" : Escape(operationKey.TenantId, nameof(operationKey.TenantId));
        var operation = Escape(operationKey.OperationName, nameof(operationKey.OperationName));
        var instance = Escape(operationKey.InstanceId, nameof(operationKey.InstanceId));
        return $"{prefix}{separator}domain:{domain}:scope:{scope}:tenant:{tenant}:operation:{operation}:instance:{instance}";
    }

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
