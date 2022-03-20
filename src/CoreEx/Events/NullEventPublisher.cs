// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading.Tasks;

namespace CoreEx.Events
{
    /// <summary>
    /// Represents a <c>null</c> event publisher; whereby the events are simply swallowed/discarded on publish.
    /// </summary>
    public class NullEventPublisher : IEventPublisher
    {
        /// <inheritdoc/>
        public Task PublishAsync(params EventData[] events) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task PublishAsync(string name, params EventData[] events)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return Task.CompletedTask;
        }
    }
}