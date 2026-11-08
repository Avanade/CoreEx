namespace CoreEx.Diagnostics;

/// <summary>
/// Provides shared validation and diagnostics for optional workflow correlation.
/// </summary>
public static class Correlation
{
    // Tracks receive nesting and activity ownership, including receives without an ExecutionContext; correlation itself belongs to ExecutionContext.
    private static readonly AsyncLocal<ReceiveScope?> _receiveScope = new();

    /// <summary>Gets the maximum correlation identifier length.</summary>
    public const int MaxLength = 256;

    /// <summary>Gets the CoreEx-defined tracing attribute name; this is not an OpenTelemetry semantic convention.</summary>
    public const string ActivityTagName = "correlation.id";

    /// <summary>Indicates whether the optional value is a valid, case-preserved HTTP-safe token.</summary>
    /// <param name="value">Null or a token of 1-256 visible ASCII characters without commas.</param>
    /// <returns>Whether the value is valid.</returns>
    public static bool IsValid(string? value)
    {
        if (value is null)
            return true;

        if (value.Length is 0 or > MaxLength)
            return false;

        foreach (var c in value)
        {
            if (c is < '!' or > '~' or ',')
                return false;
        }

        return true;
    }

    /// <summary>Validates an identifier without changing it.</summary>
    /// <param name="value">The optional identifier.</param>
    /// <returns>The original value.</returns>
    public static string? Validate(string? value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Correlation must be 1-256 visible ASCII characters without whitespace or commas.", nameof(value));

        return value;
    }

    /// <summary>Opens an execution-owned scope which reads ambient correlation at log emission.</summary>
    /// <param name="logger">The logger sharing the execution's logging provider.</param>
    /// <returns>The scope, to dispose at the execution boundary.</returns>
    /// <remarks>Enable scopes on the logging provider. Providers which snapshot scope state need their own enrichment adapter.</remarks>
    public static IDisposable? BeginScope(ILogger? logger) => logger?.BeginScope(new CorrelationScope());

    /// <summary>Imports received metadata without rejecting otherwise processable messages.</summary>
    /// <param name="value">The received identifier, including absence.</param>
    /// <param name="logger">The logger for invalid-metadata warnings.</param>
    /// <remarks>Does not create an ambient context. Invalid values are warned about without logging the raw value and are cleared.</remarks>
    public static void CaptureReceived(string? value, ILogger logger)
    {
        if (!IsValid(value))
        {
            if (logger.ThrowIfNull().IsEnabled(LogLevel.Warning))
                logger.LogWarning("Received invalid correlation metadata; correlation was cleared and message processing will continue.");

            value = null;
        }

        if (ExecutionContext.TryGetCurrent(out var ec))
            ec.CorrelationId = value;

        Activity.Current?.SetTag(ActivityTagName, value);
    }

    /// <summary>Imports message correlation within a restorable receive boundary.</summary>
    /// <param name="value">The authoritative received correlation, including absence.</param>
    /// <param name="logger">The warning logger.</param>
    /// <returns>The boundary to dispose after message processing.</returns>
    /// <remarks>Restores caller context. A top-level receive span retains its processed correlation; nested receives restore their caller's span attribute.</remarks>
    public static IDisposable BeginReceive(string? value, ILogger logger)
    {
        var scope = new ReceiveScope();
        try
        {
            CaptureReceived(value, logger);
            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    /// <summary>Owns restoration without changing activity parentage or leaking nested message state.</summary>
    private sealed class ReceiveScope : IDisposable
    {
        private readonly ExecutionContext? _context;
        private readonly string? _previous;
        private readonly Activity? _activity = Activity.Current;
        private readonly object? _previousTag;
        private readonly ReceiveScope? _parent = _receiveScope.Value;
        private bool _disposed;

        /// <summary>Captures the caller's context and parent receive boundary.</summary>
        public ReceiveScope()
        {
            ExecutionContext.TryGetCurrent(out _context);
            _previous = _context?.CorrelationId;
            _previousTag = _activity?.GetTagItem(ActivityTagName);
            _receiveScope.Value = this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            var receivedTag = _activity?.GetTagItem(ActivityTagName);
            if (_context is not null)
                _context.CorrelationId = _previous;

            _activity?.SetTag(ActivityTagName, _parent is not null && ReferenceEquals(_parent._activity, _activity) ? _previousTag : receivedTag);
            _receiveScope.Value = _parent;
        }
    }

    /// <summary>Exposes current context state when a logging provider enumerates its scopes.</summary>
    private sealed class CorrelationScope : IEnumerable<KeyValuePair<string, object?>>
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return new("CorrelationId", ExecutionContext.TryGetCurrent(out var ec) ? ec.CorrelationId : null);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => ExecutionContext.TryGetCurrent(out var ec) ? $"CorrelationId={ec.CorrelationId}" : "CorrelationId=";
    }
}
