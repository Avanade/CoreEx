﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Logging;
using System;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the result <see cref="ErrorHandler"/> options.
    /// </summary>
    public enum ErrorHandling
    {
        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs the underlying <see cref="System.Exception"/> will continue to bubble up the stack as unhandled to the executing host.
        /// </summary>
        /// <remarks>The host is the process that initiated the underlying <see cref="EventSubscriberBase"/>.</remarks>
        HandleByHost,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs that it should be handled by the subscriber.
        /// </summary>
        /// <remarks>Depending on the underlying messaging subsystem this may result in the likes of the message being deadlettered (or equivalent) where supported. May result in <see cref="HandleByHost"/> (bubble up the stack as unhandled to the executing host)
        /// where subscriber is unable to handle appropriately.
        /// <para>Where the underlying <see cref="Exception"/> implements <see cref="Abstractions.IExtendedException"/> and the <see cref="Abstractions.IExtendedException.IsTransient"/> property is set to <c>true</c> any configured retries
        /// will be performed until exhausted; then will bubble up the stack as unhandled to the executing host.</para></remarks>
        HandleBySubscriber,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs that it may be transient and should be retried (where possible).
        /// </summary>
        /// <remarks>Results in a <see cref="EventSubscriberException"/> where the <see cref="Abstractions.IExtendedException.IsTransient"/> property is set (overridden) to <c>true</c>.
        /// <para>A <i>retry</i> will <i>only</i> occur where the <see cref="EventSubscriberBase"/> implementation supports/enables.</para></remarks>
        Retry,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs this is expected and the current event/message should be completed without further processing and logging (i.e. silently).
        /// </summary>
        /// <remarks>A <see cref="LogLevel.Debug"/> will be logged where applicable to support debugging.</remarks>
        CompleteAsSilent,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs this is expected and should be completed without further processing after logging as <see cref="LogLevel.Information"/>.
        /// </summary>
        CompleteWithInformation,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs this is expected and should be completed without further processing after logging as <see cref="LogLevel.Warning"/>.
        /// </summary>
        CompleteWithWarning,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs this is expected and should be completed without further processing after logging as <see cref="LogLevel.Error"/>.
        /// </summary>
        CompleteWithError,

        /// <summary>
        /// Indicates that when the corresponding <i>error</i> occurs the <see cref="ErrorHandler.FailFast(EventSubscriberException)"/> is invoked to immediately terminate the underlying process.
        /// </summary>
        /// <remarks><i>Note:</i> this <b>must</b> be tested thoroughly by the developer to ensure that there are no negative side-effects of the process terminating; equally, the <see cref="ErrorHandler.FailFast(EventSubscriberException)"/>
        /// may need to be overridden to achieve the desired outcome. Before termination the error will be logged as <see cref="LogLevel.Critical"/>.</remarks>
        CriticalFailFast
    }
}