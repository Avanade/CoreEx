// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Hosting;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Represents the status of a <see cref="TimerHostedServiceBase"/>.
    /// </summary>
    public enum TimerHostedServiceStatus
    {
        /// <summary>
        /// Initialized, but not started.
        /// </summary>
        Initialized,

        /// <summary>
        /// Starting; i.e. <see cref="IHostedService.StartAsync"/> has been called.
        /// </summary>
        Starting,

        /// <summary>
        /// Sleeping; i.e. the timer is waiting for the next interval.
        /// </summary>
        Sleeping,

        /// <summary>
        /// Running; i.e. the timer is executing the <see cref="TimerHostedServiceBase.ExecuteAsync"/> method.
        /// </summary>
        Running,

        /// <summary>
        /// Stopping; i.e. <see cref="IHostedService.StopAsync"/> has been called.
        /// </summary>
        Stopping,

        /// <summary>
        /// Stopped; the service has been stopped.
        /// </summary>
        Stopped
    }
}