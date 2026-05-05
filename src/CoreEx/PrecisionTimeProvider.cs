namespace CoreEx;

/// <summary>
/// Provides a <see cref="TimeProvider"/> that truncates <see cref="DateTimeOffset"/> to a specified <i>precision</i>.
/// </summary>
/// <param name="decimalPlaces">The number of decimal places (0-7) for fractional seconds. Defaults to 6 (microseconds) for database compatibility.</param>
/// <param name="innerProvider">The inner <see cref="TimeProvider"/> to wrap. Defaults to <see cref="TimeProvider.System"/>.</param>
/// <remarks>This is useful for ensuring consistent timestamp precision across different database systems:
/// <list type="bullet">
///   <item><description>PostgreSQL <c>timestamptz</c>: 6 decimal places (microseconds).</description></item>
///   <item><description>SQL Server <c>datetimeoffset(6)</c>: 6 decimal places (microseconds).</description></item>
///   <item><description>SQL Server <c>datetimeoffset(7)</c>: 7 decimal places (100 nanoseconds) - default.</description></item>
/// </list>
/// </remarks>
public sealed class PrecisionTimeProvider(int decimalPlaces = 6, TimeProvider? innerProvider = null) : TimeProvider
{
    private readonly TimeProvider _innerProvider = innerProvider ?? System;
    private readonly int _decimalPlaces = decimalPlaces >= 0 && decimalPlaces <= 7 ? decimalPlaces : throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Must be between 0 and 7.");
    private readonly long _tickDivisor = (long)Math.Pow(10, 7 - decimalPlaces);

    /// <summary>
    /// Gets the number of decimal places for fractional seconds.
    /// </summary>
    public int DecimalPlaces => _decimalPlaces;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow()
    {
        var now = _innerProvider.GetUtcNow();
        var ticks = now.Ticks;
        var truncatedTicks = ticks - (ticks % _tickDivisor);
        return new DateTimeOffset(truncatedTicks, now.Offset);
    }

    /// <inheritdoc/>
    public override long GetTimestamp() => _innerProvider.GetTimestamp();

    /// <inheritdoc/>
    public override TimeZoneInfo LocalTimeZone => _innerProvider.LocalTimeZone;

    /// <inheritdoc/>
    public override long TimestampFrequency => _innerProvider.TimestampFrequency;
}