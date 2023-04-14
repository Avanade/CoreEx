// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;
using System.Threading;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Represents an <see cref="IEventSubscriber"/> with no <see cref="ValueType"/> (supports <see cref="EventData"/>).
    /// </summary>
    /// <remarks>This is for use when the <see cref="EventData"/> has no corresponding <see cref="EventData.Value"/>; or the <see cref="EventData.Value"/> does not need to be deserialized.</remarks>
    public abstract class SubscriberBase : IEventSubscriber
    {
        /// <inheritdoc/>
        public virtual Type EventDataType => typeof(EventData);

        /// <inheritdoc/>
        public virtual Type? ValueType => null;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.None"/> indicating that the <see cref="EventSubscriberBase.UnhandledHandling"/> configuration will be used; override explicitly to set specific handling behaviour where applicable.</remarks>
        public virtual ErrorHandling UnhandledHandling => ErrorHandling.None;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.None"/> indicating that the <see cref="EventSubscriberBase.SecurityHandling"/> configuration will be used; override explicitly to set specific handling behaviour where applicable.</remarks>
        public virtual ErrorHandling SecurityHandling => ErrorHandling.None;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.None"/> indicating that the <see cref="EventSubscriberBase.TransientHandling"/> configuration will be used; override explicitly to set specific handling behaviour where applicable.</remarks>
        public virtual ErrorHandling TransientHandling => ErrorHandling.None;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.None"/> indicating that the <see cref="EventSubscriberBase.NotFoundHandling"/> configuration will be used; override explicitly to set specific handling behaviour where applicable.</remarks>
        public virtual ErrorHandling NotFoundHandling => ErrorHandling.None;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.None"/> indicating that the <see cref="EventSubscriberBase.ConcurrencyHandling"/> configuration will be used; override explicitly to set specific handling behaviour where applicable.</remarks>
        public virtual ErrorHandling ConcurrencyHandling => ErrorHandling.None;

        /// <inheritdoc/>
        /// <remarks>Defaults to <see cref="ErrorHandling.None"/> indicating that the <see cref="EventSubscriberBase.InvalidDataHandling"/> configuration will be used; override explicitly to set specific handling behaviour where applicable.</remarks>
        public virtual ErrorHandling InvalidDataHandling => ErrorHandling.None;

        /// <inheritdoc/>
        public abstract Task ReceiveAsync(EventData @event, CancellationToken cancellationToken);
    }
}