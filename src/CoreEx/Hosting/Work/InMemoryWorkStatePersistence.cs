// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Hosting.Work
{
    /// <summary>
    /// An <see cref="IWorkStatePersistence"/> that persists the <see cref="WorkState"/> in-memory which can be used for the likes of testing.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public class InMemoryWorkStatePersistence(ILogger<InMemoryWorkStatePersistence>? logger = null) : IWorkStatePersistence
    {
        private readonly Dictionary<string, WorkState> _workStates = [];
        private readonly Dictionary<string, BinaryData> _workData = [];
        private readonly ILogger? _logger = logger;

        /// <summary>
        /// Gets all the <see cref="WorkState"/> entries.
        /// </summary>
        public WorkState[] GetWorkStates() => _workStates.Values.ToArray();

        /// <inheritdoc/>
        public Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult(_workStates.TryGetValue(id, out var state) ? state : null);

        /// <inheritdoc/>
        public Task CreateAsync(WorkState state, CancellationToken cancellationToken)
        {
            if (_workStates.ContainsKey(state.Id.ThrowIfNull()))
                throw new ArgumentException("Create can not be performed as the WorkState already exists; the type and identifier combination should be unique.", nameof(state));

            _logger?.LogDebug("Creating WorkState: {Id}", state.Id);
            _workStates.Add(state.Id, state);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task UpdateAsync(WorkState state, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Updating WorkState: {Id}", state.Id);
            _workStates[state.Id.ThrowIfNull()] = state;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Deleting WorkState: {Id}", id);
            _workStates.Remove(id);
            _workData.Remove(id);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Getting WorkState data for: {Id}", id);
            return Task.FromResult(_workData.TryGetValue(id, out var data) ? data : null);
        }

        /// <inheritdoc/>
        public Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Setting WorkState data for: {Id}", id);
            _workData[id] = data;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears the in-memory persistence.
        /// </summary>
        public void Clear()
        {
            _logger?.LogDebug("Clearing WorkState persistence.");
            _workStates.Clear();
            _workData.Clear();
        }
    }
}