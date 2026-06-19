namespace CoreEx.Hosting;

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class Extensions
{
    extension(ServiceStatus status)
    {
        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in its initial state.
        /// </summary>
        /// <remarks>Being <see cref="ServiceStatus.Initializing"/>.</remarks>
        public bool IsInitializing => status == ServiceStatus.Initializing;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a pause state.
        /// </summary>
        /// <remarks>Being either <see cref="ServiceStatus.Pausing"/> or <see cref="ServiceStatus.Paused"/>.</remarks>
        public bool IsPause => status == ServiceStatus.Pausing || status == ServiceStatus.Paused;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a stop state.
        /// </summary>
        /// <remarks>Being either <see cref="ServiceStatus.Stopping"/> or <see cref="ServiceStatus.Stopped"/>.</remarks>
        public bool IsStop => status == ServiceStatus.Stopping || status == ServiceStatus.Stopped;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a sleep state.
        /// </summary>
        /// <remarks>Being <see cref="ServiceStatus.Sleeping"/>.</remarks>
        public bool IsAsleep => status == ServiceStatus.Sleeping;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a running state.
        /// </summary>
        /// <remarks>Being <see cref="ServiceStatus.Running"/>.</remarks>
        public bool IsRunning => status == ServiceStatus.Running;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a state that the service can be started.
        /// </summary>
        /// <remarks>Note that the service can only be started from the <see cref="ServiceStatus.Initializing"/> state.</remarks>
        public bool CanStart => status.IsInitializing;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a state that the service can be paused.
        /// </summary>
        /// <remarks>Note that the service can be paused from either the <see cref="ServiceStatus.Sleeping"/> or <see cref="ServiceStatus.Running"/> states.</remarks>
        public bool CanPause => status == ServiceStatus.Sleeping || status == ServiceStatus.Running;

        /// <summary>
        /// Indicates whether the <paramref name="status"/> is in a state that the service can be resumed.
        /// </summary>
        /// <remarks>Note that the service can only be resumed from the <see cref="ServiceStatus.Paused"/> state.</remarks>
        public bool CanResume => status == ServiceStatus.Paused;
    }
}