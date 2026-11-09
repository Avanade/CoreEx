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

    /// <summary>Indicates whether automatic workflow correlation is enabled.</summary>
    /// <param name="configuration">The optional configuration; defaults to the current execution's service provider.</param>
    /// <returns>The configured value of <c>CoreEx:Correlation:Enabled</c>, defaulting to true.</returns>
    /// <remarks>Configure consistently at host startup. Without supplied or ambient configuration, correlation is enabled.</remarks>
    public static bool IsEnabled(IConfiguration? configuration = null) => Internal.GetConfigurationValue("CoreEx:Correlation:Enabled", true, configuration);

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

    /// <summary>Opens a scope that snapshots the current correlation.</summary>
    /// <param name="logger">The logger sharing the execution's logging provider.</param>
    /// <returns>The scope, to dispose after logging.</returns>
    /// <remarks>Enable scopes on the logging provider. The immutable snapshot remains correct when a provider buffers scope objects for asynchronous export.</remarks>
    public static IDisposable? BeginScope(ILogger? logger)
        => IsEnabled() ? logger?.BeginScope(new CorrelationScope(ExecutionContext.TryGetCurrent(out var ec) ? ec.CorrelationId : null)) : null;

    /// <summary>Imports received metadata without rejecting otherwise processable messages.</summary>
    /// <param name="value">The received identifier, including absence.</param>
    /// <param name="logger">The logger for invalid-metadata warnings.</param>
    /// <remarks>Does not create an ambient context. Invalid values are warned about without logging the raw value and are cleared.</remarks>
    public static void CaptureReceived(string? value, ILogger logger)
    {
        if (!IsEnabled())
            return;

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

    /// <summary>Imports message correlation within a restorable receive and logging boundary.</summary>
    /// <param name="value">The authoritative received correlation, including absence.</param>
    /// <param name="logger">The warning logger.</param>
    /// <returns>The boundary to dispose after message processing, or null when automatic correlation is disabled.</returns>
    /// <remarks>Opens a correlation logging scope after capture and restores caller context on disposal. A top-level receive span retains its processed correlation; nested receives restore their caller's span attribute.</remarks>
    public static IDisposable? BeginReceive(string? value, ILogger logger)
    {
        if (!IsEnabled())
            return null;

        var scope = new ReceiveScope();
        try
        {
            CaptureReceived(value, logger);
            scope.SetLoggingScope(BeginScope(logger));
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
        private IDisposable? _loggingScope;
        private bool _disposed;

        /// <summary>Captures the caller's context and parent receive boundary.</summary>
        public ReceiveScope()
        {
            ExecutionContext.TryGetCurrent(out _context);
            _previous = _context?.CorrelationId;
            _previousTag = _activity?.GetTagItem(ActivityTagName);
            _receiveScope.Value = this;
        }

        /// <summary>Owns the immutable correlation scope used for logging during this receive.</summary>
        /// <param name="loggingScope">The logger scope captured after correlation import.</param>
        public void SetLoggingScope(IDisposable? loggingScope) => _loggingScope = loggingScope;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            try
            {
                _loggingScope?.Dispose();
            }
            finally
            {
                var receivedTag = _activity?.GetTagItem(ActivityTagName);
                if (_context is not null)
                    _context.CorrelationId = _previous;

                _activity?.SetTag(ActivityTagName, _parent is not null && ReferenceEquals(_parent._activity, _activity) ? _previousTag : receivedTag);
                _receiveScope.Value = _parent;
            }
        }
    }

    /// <summary>Exposes the immutable correlation value captured when a logging scope begins.</summary>
    private sealed class CorrelationScope(string? correlationId) : IEnumerable<KeyValuePair<string, object?>>
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return new("CorrelationId", correlationId);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => $"CorrelationId={correlationId}";
    }
}
