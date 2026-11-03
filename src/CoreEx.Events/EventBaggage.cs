namespace CoreEx.Events;

/// <summary>
/// Provides bounded W3C baggage parsing and activity import shared by event envelopes and transports.
/// </summary>
public static class EventBaggage
{
    /// <summary>
    /// Gets the maximum encoded baggage header size in UTF-8 bytes.
    /// </summary>
    public const int MaximumBytes = 8192;

    /// <summary>
    /// Gets the maximum number of parsed entries considered per import.
    /// </summary>
    public const int MaximumEntries = 64;

    private static readonly BaggagePropagator _propagator = new();

    /// <summary>
    /// Parses a bounded W3C header, ignoring oversized or malformed baggage without rejecting event data.
    /// </summary>
    /// <param name="header">The encoded baggage header.</param>
    /// <returns>The parsed bounded entries.</returns>
    public static IEnumerable<KeyValuePair<string, string?>> Parse(string? header)
    {
        if (string.IsNullOrEmpty(header) || Encoding.UTF8.GetByteCount(header) > MaximumBytes)
            return [];

        var context = _propagator.Extract(default, header, (value, key) => key == "baggage" ? [value] : []);
        return Baggage.GetBaggage(context.Baggage).Take(MaximumEntries).Select(item => new KeyValuePair<string, string?>(item.Key, item.Value)).ToArray();
    }

    /// <summary>
    /// Imports bounded entries without overwriting baggage keys already present on the host activity.
    /// </summary>
    /// <param name="activity">The host activity; a no-op where null.</param>
    /// <param name="baggage">The decoded baggage entries.</param>
    public static void Import(Activity? activity, IEnumerable<KeyValuePair<string, string?>>? baggage)
    {
        if (activity is null || baggage is null)
            return;

        var existingKeys = activity.Baggage.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        var bytes = 0;
        foreach (var item in baggage.Take(MaximumEntries))
        {
            if (string.IsNullOrEmpty(item.Key))
                continue;

            bytes += Encoding.UTF8.GetByteCount(Uri.EscapeDataString(item.Key)) + Encoding.UTF8.GetByteCount(Uri.EscapeDataString(item.Value ?? "")) + (bytes == 0 ? 1 : 2);
            if (bytes > MaximumBytes)
                break;

            if (existingKeys.Add(item.Key))
                activity.AddBaggage(item.Key, item.Value);
        }
    }
}
