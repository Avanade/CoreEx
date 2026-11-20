namespace CoreEx.Redis;

/// <summary>
/// Configures a <see cref="RedisWorkProvider"/>.
/// </summary>
public sealed class RedisWorkProviderOptions
{
    /// <summary>Gets or sets the key prefix; defaults to <c>CoreEx:work:v2:</c>. A colon is appended when necessary before the identity segments.</summary>
    public string KeyPrefix { get; set; } = "CoreEx:work:v2:";

    /// <summary>Gets or sets the maximum duration to wait for each Redis operation; defaults to five seconds.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the optional clock override; unset uses Redis server time.</summary>
    /// <remarks>Using a local clock in a multi-host production deployment reintroduces cross-host clock skew.</remarks>
    public TimeProvider? TimeProvider { get; set; }

    /// <summary>Validates the options.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(KeyPrefix))
            throw new ArgumentException("The Redis work key prefix is required.", nameof(KeyPrefix));

        RedisOperationKey.ValidateUnicode(KeyPrefix, nameof(KeyPrefix));

        if (OperationTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(OperationTimeout));
    }
}
