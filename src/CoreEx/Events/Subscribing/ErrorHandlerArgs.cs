// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Hosting.Work;
using Microsoft.Extensions.Logging;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the <see cref="ErrorHandler.HandleErrorAsync"/> arguments.
    /// </summary>
    /// <param name="identifier">The corresponding unique identifier.</param>
    /// <param name="eventSubscriberException">The <see cref="EventSubscriberException"/>.</param>
    /// <param name="errorHandling">The <see cref="Subscribing.ErrorHandling"/> option.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public sealed class ErrorHandlerArgs(string? identifier, EventSubscriberException eventSubscriberException, ErrorHandling errorHandling, ILogger logger)
    {
        /// <summary>
        /// Gets the corresponding unique identifier.
        /// </summary>
        public string? Identifier { get; } = identifier;

        /// <summary>
        /// Gets the <see cref="EventSubscriberException"/>.
        /// </summary>
        public EventSubscriberException Exception { get; } = eventSubscriberException.ThrowIfNull(nameof(eventSubscriberException));

        /// <summary>
        /// Gets the <see cref="Subscribing.ErrorHandling"/> option.
        /// </summary>
        public ErrorHandling ErrorHandling { get; } = errorHandling;
        
        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        public ILogger Logger { get; } = logger.ThrowIfNull(nameof(logger));
        
        /// <summary>
        /// Gets or sets the optional <see cref="IEventSubscriberInstrumentation"/>.
        /// </summary>
        public IEventSubscriberInstrumentation? Instrumentation { get; set; }

        /// <summary>
        /// Gets or sets the optional <see cref="WorkOrchestrator"/>.
        /// </summary>
        public WorkStateOrchestrator? WorkOrchestrator { get; set; }
    }
}