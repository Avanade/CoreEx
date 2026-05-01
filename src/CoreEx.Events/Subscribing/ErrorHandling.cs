namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the result <see cref="ErrorHandler"/> options.
/// </summary>
public enum ErrorHandling
{
    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs that no specific handling should occur and will result in an <see cref="EventSubscriberUnhandledException"/> which will bubble back up the stack for the invoking host to handle.
    /// </summary>
    /// <remarks>A <see cref="LogLevel.Debug"/> message will be logged, where applicable, to support debugging.</remarks>
    None,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs this is expected and the current event/message should be completed without further processing (i.e. silently).
    /// </summary>
    /// <remarks>A <see cref="LogLevel.Debug"/> message will be logged, where applicable, to support debugging.</remarks>
    CompleteAsSilent,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs this is expected and should be completed without further processing after logging as <see cref="LogLevel.Information"/>.
    /// </summary>
    CompleteAsInformation,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs this is expected and should be completed without further processing after logging as <see cref="LogLevel.Warning"/>.
    /// </summary>
    CompleteAsWarning,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs this is expected and should be completed without further processing after logging as <see cref="LogLevel.Error"/>.
    /// </summary>
    CompleteAsError,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs that it may be transient and should be retried (where possible).
    /// </summary>
    /// <remarks>A <see cref="LogLevel.Debug"/> message will be logged, where applicable, to support debugging.</remarks>
    Retry,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs the current event/message should be forwarded as a dead-letter without further processing.
    /// </summary>
    /// <remarks>A <see cref="LogLevel.Debug"/> message will be logged, where applicable, to support debugging.</remarks>
    DeadLetter,

    /// <summary>
    /// Indicates that when the corresponding <i>error</i> occurs that it is considered <b>catastrophic</b> and will result in an <see cref="EventSubscriberCatastrophicException"/>) which will bubble back up the stack for the invoking host to handle.
    /// </summary>
    /// <remarks>A <see cref="LogLevel.Critical"/> message will be logged.</remarks>
    Catastrophic
}