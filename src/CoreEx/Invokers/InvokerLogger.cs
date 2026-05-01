namespace CoreEx.Invokers;

/// <summary>
/// Provides high-performance logging for invokers.
/// </summary>
internal static partial class InvokerLogger
{
    [LoggerMessage(Message = "{invoker}: {operation} {status}.")]
    public static partial void InvokeStart(this ILogger logger, LogLevel logLevel, string invoker, string operation, string status);

    [LoggerMessage(Message = "{invoker}: {operation} {status} - {error}. [{elapsed}ms]")]
    public static partial void InvokeError(this ILogger logger, LogLevel logLevel, string invoker, string operation, string status, string? error, double elapsed);

    [LoggerMessage(Message = "{invoker}: {operation} {status} - {error}: {exception} [{elapsed}ms]")]
    public static partial void InvokeException(this ILogger logger, LogLevel logLevel, string invoker, string operation, string status, string? error, string exception, double elapsed);

    [LoggerMessage(Message = "{invoker}: {operation} {status}. [{elapsed}ms]")]
    public static partial void InvokeComplete(this ILogger logger, LogLevel logLevel, string invoker, string operation, string status, double elapsed);

    [LoggerMessage(Message = "{invoker}: {operation} {status} - {context}")]
    public static partial void InvokeContext(this ILogger logger, LogLevel logLevel, string invoker, string operation, string status, string context);
}