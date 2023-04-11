// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides additional context related to the source of the <see cref="EventSubscriberException"/>.
    /// </summary>
    public enum EventSubscriberExceptionSource
    {
        /// <summary>
        /// Indicates that the <see cref="EventSubscriberException"/> relates to the execution of the underlying <i>subscriber</i> receiving as implemented by the consumer (see also <seealso cref="IErrorHandling"/>).
        /// </summary>
        Subscriber = 2201,

        /// <summary>
        /// Indicates that the <see cref="EventSubscriberException"/> relates to the <see cref="EventSubscriberBase.EventDataDeserializationErrorHandling"/>.
        /// </summary>
        EventDataDeserialization = 2202,

        /// <summary>
        /// Indicates that the <see cref="EventSubscriberException"/> relates to the <see cref="EventSubscriberOrchestrator.NotSubscribedHandling"/>. 
        /// </summary>
        OrchestratorNotSubscribed = 2203,

        /// <summary>
        /// Indicates that the <see cref="EventSubscriberException"/> relates to the <see cref="EventSubscriberOrchestrator.AmbiquousSubscriberHandling"/>. 
        /// </summary>
        OrchestratorAmbiquousSubscriber = 2204
    }
}