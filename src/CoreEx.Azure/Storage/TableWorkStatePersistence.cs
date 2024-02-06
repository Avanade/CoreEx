// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure;
using Azure.Data.Tables;
using CoreEx.Hosting.Work;
using CoreEx.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.Storage
{
    /// <summary>
    /// An <see cref="IWorkStatePersistence"/> that persists the <see cref="WorkState"/> using an Azure <see cref="TableClient"/> and the related <see cref="BinaryData"/> to a separate <see cref="TableClient"/>.
    /// </summary>
    /// <remarks>The maximum <see cref="BinaryData"/> size currently supported is 960,000 bytes.</remarks>
    public class TableWorkStatePersistence : IWorkStatePersistence
    {
        private static readonly string[] _columns = [nameof(WorkState.TypeName), nameof(WorkState.CorrelationId), nameof(WorkState.Status), nameof(WorkState.Created), nameof(WorkState.Expiry), nameof(WorkState.Started), nameof(WorkState.Indeterminate), nameof(WorkState.Finished), nameof(WorkState.Reason)];

        private readonly TableClient _workStateTableClient;
        private readonly TableClient _workDataTableClient;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableWorkStatePersistence"/> class.
        /// </summary>
        /// <param name="tableServiceClient">The <see cref="TableServiceClient"/>.</param>
        /// <param name="workStateTableName">The work state table name.</param>
        /// <param name="workDataTableName">The work data table name.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/>.</param>
        public TableWorkStatePersistence(TableServiceClient tableServiceClient, string workStateTableName = "workstate", string workDataTableName = "workdata", IJsonSerializer? jsonSerializer = null)
            : this(tableServiceClient.ThrowIfNull(nameof(tableServiceClient)).GetTableClient(workStateTableName), tableServiceClient.GetTableClient(workDataTableName), jsonSerializer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableWorkStatePersistence"/> class.
        /// </summary>
        /// <param name="workStateTableClient">The work state <see cref="TableClient"/>.</param>
        /// <param name="workDataTableClient">The work data <see cref="TableClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/>.</param>
        public TableWorkStatePersistence(TableClient workStateTableClient, TableClient workDataTableClient, IJsonSerializer? jsonSerializer = null)
        {
            if (workStateTableClient.ThrowIfNull(nameof(workStateTableClient)).Name == workDataTableClient.ThrowIfNull(nameof(workDataTableClient)).Name)
                throw new ArgumentException("The work state and data table names must be different.", nameof(workDataTableClient));

            _workDataTableClient = workDataTableClient;
            _workStateTableClient = workStateTableClient;

            _workDataTableClient.CreateIfNotExists();
            _workStateTableClient.CreateIfNotExists();

            _jsonSerializer = jsonSerializer ?? JsonSerializer.Default;
        }

        private class WorkStateEntity() : WorkState, ITableEntity
        {
            public WorkStateEntity(WorkState state) : this()
            {
                RowKey = state.Id!;
                TypeName = state.TypeName;
                Key = state.Key;
                CorrelationId = state.CorrelationId;
                Status = state.Status;
                Created = state.Created;
                Expiry = state.Expiry;
                Started = state.Started;
                Indeterminate = state.Indeterminate;
                Finished = state.Finished;
                Reason = state.Reason;
            }

            public string PartitionKey { get; set; } = GetPartitionKey();
            public string RowKey { get; set; } = null!;
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }

        private class WorkDataEntity() : ITableEntity
        {
            private const int _chunkSize = 64000;
            private const int _maxChunks = 15;
            private const int _maxSize = _chunkSize * _maxChunks;
            private readonly BinaryData?[] _data = new BinaryData?[_maxChunks];
            public WorkDataEntity(BinaryData data) : this()
            {
                var arr = data.ToArray();
                if (arr.Length <= _chunkSize)
                {
                    Data00 = data;
                    return;
                }
                else if (arr.Length > _maxSize)
                    throw new ArgumentException($"Data too large ({arr.Length}B versus {_maxSize}B) to store in Azure table storage.", nameof(data));

                var i = 0;
                foreach (var chunk in data.ToArray().Chunk(_chunkSize))
                {
                    _data[i++] = BinaryData.FromBytes(chunk);
                }
            }
            public string PartitionKey { get; set; } = GetPartitionKey();
            public string RowKey { get; set; } = null!;
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
            public BinaryData? Data00 { get => _data[0]; set => _data[0] = value; }
            public BinaryData? Data01 { get => _data[1]; set => _data[1] = value; }
            public BinaryData? Data02 { get => _data[2]; set => _data[2] = value; }
            public BinaryData? Data03 { get => _data[3]; set => _data[3] = value; }
            public BinaryData? Data04 { get => _data[4]; set => _data[4] = value; }
            public BinaryData? Data05 { get => _data[5]; set => _data[5] = value; }
            public BinaryData? Data06 { get => _data[6]; set => _data[6] = value; }
            public BinaryData? Data07 { get => _data[7]; set => _data[7] = value; }
            public BinaryData? Data08 { get => _data[8]; set => _data[8] = value; }
            public BinaryData? Data09 { get => _data[9]; set => _data[9] = value; }
            public BinaryData? Data10 { get => _data[10]; set => _data[10] = value; }
            public BinaryData? Data11 { get => _data[11]; set => _data[11] = value; }
            public BinaryData? Data12 { get => _data[12]; set => _data[12] = value; }
            public BinaryData? Data13 { get => _data[13]; set => _data[13] = value; }
            public BinaryData? Data14 { get => _data[14]; set => _data[14] = value; }

            /// <summary>
            /// Unchunks the data properties into a single <see cref="BinaryData"/>.
            /// </summary>
            /// <returns>The <see cref="BinaryData"/>.</returns>
            public BinaryData? ToSingleData()
            {
                if (Data00 is null || Data01 is null)
                    return Data00;

                using var ms = new MemoryStream();
                for (int i = 0; i < _maxChunks; i++)
                {
                    if (_data[i] is null)
                        break;

                    ms.Write(_data[i]!.ToArray());
                }

                ms.Position = 0;
                return BinaryData.FromStream(ms);
            }
        }

        /// <summary>
        /// Gets the partition key.
        /// </summary>
        private static string GetPartitionKey() => (ExecutionContext.HasCurrent ? ExecutionContext.Current.TenantId : null) ?? "default";

        /// <inheritdoc/>
        public async Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken)
        {
            var er = await _workStateTableClient.GetEntityIfExistsAsync<WorkStateEntity>(GetPartitionKey(), id, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!er.HasValue)
                return null;

            return new WorkState
            {
                Id = er.Value!.RowKey,
                TypeName = er.Value.TypeName,
                Key = er.Value.Key,
                CorrelationId = er.Value.CorrelationId,
                Status = er.Value.Status,
                Created = er.Value.Created,
                Expiry = er.Value.Expiry,
                Started = er.Value.Started,
                Indeterminate = er.Value.Indeterminate,
                Finished = er.Value.Finished,
                Reason = er.Value.Reason
            };
        }

        /// <inheritdoc/>
        public Task CreateAsync(WorkState state, CancellationToken cancellationToken) => UpsertAsync(state, cancellationToken);

        /// <inheritdoc/>
        public Task UpdateAsync(WorkState state, CancellationToken cancellationToken) => UpsertAsync(state, cancellationToken);

        /// <summary>
        /// Performs an upsert (create/update).
        /// </summary>
        private async Task UpsertAsync(WorkState state, CancellationToken cancellationToken)  
            => await _workStateTableClient.UpsertEntityAsync(new WorkStateEntity(state), TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            await _workDataTableClient.DeleteEntityAsync(GetPartitionKey(), id, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _workStateTableClient.DeleteEntityAsync(GetPartitionKey(), id, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken)
        {
            var er = await _workDataTableClient.GetEntityIfExistsAsync<WorkDataEntity>(GetPartitionKey(), id, cancellationToken: cancellationToken).ConfigureAwait(false);
            return er.HasValue ? er.Value!.ToSingleData() : null;
        }

        /// <inheritdoc/>
        public Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken)
            => _workDataTableClient.UpsertEntityAsync(new WorkDataEntity(data) { PartitionKey = GetPartitionKey(), RowKey = id }, TableUpdateMode.Replace, cancellationToken: cancellationToken);
    }
}