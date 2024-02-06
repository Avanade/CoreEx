// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Hosting.Work;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events.Subscribing
{
    /// <summary>
    /// Provides the <see cref="EventSubscriberBase"/> arguments; is obstensibly a <see cref="Dictionary{TKey, TValue}"/> with a <see cref="string"/> key and <see cref="object"/> value.
    /// </summary>
    /// <remarks>This enables runtime state to passed through to the underlying subscriber receive logic where applicable.</remarks>
    public class EventSubscriberArgs : Dictionary<string, object?> 
    {
        private EventSubscriberBase? _owner;
        private string? _id;
        private WorkState? _workState;

        /// <summary>
        /// Sets the initial subscriber state.
        /// </summary>
        /// <param name="owner">The owning <see cref="EventSubscriberBase"/>.</param>
        /// <param name="id">The event/messagage identifier.</param>
        /// <param name="workState">The corresponding <see cref="WorkState"/> (where applicable).</param>
        internal void SetState(EventSubscriberBase owner, string id, WorkState? workState)
        {
            if (_owner is not null)
                throw new InvalidOperationException($"An existing {nameof(EventSubscriberArgs)} instance may not be reused across subscriber executions; a new instance is required per event/message.");

            _owner = owner.ThrowIfNull(nameof(owner));
            _id = id.ThrowIfNullOrEmpty(nameof(id));
            _workState = workState;
        }

        /// <summary>
        /// Gets the owning <see cref="EventSubscriberBase"/>.
        /// </summary>
        /// <remarks>This is set automatically by the <see cref="EventSubscriberBase"/>.</remarks>
        public EventSubscriberBase Owner => _owner ?? throw new InvalidOperationException($"The {nameof(Owner)} property has not yet been configured by the {nameof(EventSubscriberBase)} infrastructure.");

        /// <summary>
        /// Gets the event/messagage identifier.
        /// </summary>
        public string Id => _id ?? throw new InvalidOperationException($"The {nameof(Id)} property has not yet been configured by the {nameof(EventSubscriberBase)} infrastructure.");

        /// <summary>
        /// Gets the <see cref="WorkState"/> (where applicable).
        /// </summary>
        public WorkState? WorkState => _workState ?? throw new InvalidOperationException($"The {nameof(WorkState)} property has not yet been configured by the {nameof(EventSubscriberBase)} infrastructure.");

        /// <summary>
        /// Indicates whether the subscribing event/message has corresponding <see cref="WorkState"/>.
        /// </summary>
        public bool HasWorkState => _workState != null;

        /// <summary>
        /// Sets the corresponding <see cref="WorkState"/> result data with the specified <paramref name="data"/> (where <see cref="HasWorkState"/>).
        /// </summary>
        /// <param name="data">The <see cref="BinaryData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task SetWorkStateDataAsync(BinaryData data, CancellationToken cancellationToken = default)
        {
            if (!HasWorkState)
                throw new InvalidOperationException($"This event/message is not being {nameof(WorkState)} tracked/orchestrated therefore tracking data is unable to be set.");

            return Owner.WorkStateOrchestrator!.SetDataAsync(Id, data, cancellationToken);
        }

        /// <summary>
        /// Sets the corresponding <see cref="WorkState"/> result data with the specified <paramref name="value"/> serialized as JSON (where <see cref="HasWorkState"/>).
        /// </summary>
        /// <param name="value">The value to JSON serialize as the result data.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task SetWorkStateDataAsync<TValue>(TValue value, CancellationToken cancellationToken = default)
        {
            if (!HasWorkState)
                throw new InvalidOperationException($"This event/message is not being {nameof(WorkState)} tracked/orchestrated therefore tracking data is unable to be set.");

            return Owner.WorkStateOrchestrator!.SetDataAsync(Id, value, cancellationToken);
        }
    }
}