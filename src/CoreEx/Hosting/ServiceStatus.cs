namespace CoreEx.Hosting;

/// <summary>
/// Represents the status of a service; for example, <see cref="HostedServiceBase"/> and <see cref="TimerHostedServiceBase"/>.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// Initializing, but not started.
    /// </summary>
    Initializing,

    /// <summary>
    /// No-op; i.e. the service has been explicitly requested to not perform any work.
    /// </summary>
    NoOp,

    /// <summary>
    /// Starting; i.e. the start has been initiated.
    /// </summary>
    Starting,

    /// <summary>
    /// Sleeping; the service is in between executions.
    /// </summary>
    Sleeping,

    /// <summary>
    /// Running; the service is executing work.
    /// </summary>
    Running,

    /// <summary>
    /// Pausing; i.e. the pause has been initiated.
    /// </summary>
    Pausing,

    /// <summary>
    /// Paused; the service is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Resuming; i.e. the resume has been initiated.
    /// </summary>
    /// <remarks>There is no <i>Resumed</i> status; should return to either <see cref="Sleeping"/> or <see cref="Running"/>.</remarks>
    Resuming,

    /// <summary>
    /// Stopping; i.e. the stop has been initiated.
    /// </summary>
    Stopping,

    /// <summary>
    /// Stopped; the service is stopped.
    /// </summary>
    Stopped
}