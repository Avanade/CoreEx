// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Enables instrumentation for an <see cref="SubscriberBase"/>.
    /// </summary>
    /// <remarks>See also <see cref="EventSubscriberInstrumentationBase"/>.</remarks>
    public interface IEventSubscriberInstrumentation
    {
        /// <summary>
        /// Records instrumentation based on the <paramref name="errorHandling"/> and <paramref name="exception"/> values.
        /// </summary>
        /// <param name="errorHandling">The corresponding <see cref="ErrorHandling"/> value where an error ocurred; otherwise, <c>null</c> for success.</param>
        /// <param name="exception">The corresponding <see cref="Exception"/> where there is an error (will be of Type <see cref="EventSubscriberException"/> where <paramref name="errorHandling"/> is not <see cref="ErrorHandling.HandleByHost"/>).</param>
        void Instrument(ErrorHandling? errorHandling = null, Exception? exception = null);
    }
}