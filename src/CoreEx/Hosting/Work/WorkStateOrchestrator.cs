// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Hosting.Work
{
    /// <summary>
    /// Represents the long-running work (see <see cref="WorkState"/>) tracking orchestrator.
    /// </summary>
    /// <param name="persistence">The <see cref="IWorkStatePersistence"/>.</param>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>.</param>
    /// <param name="identifierGenerator">The <see cref="IIdentifierGenerator"/>.</param>
    /// <remarks>There are some basic consistency checks that occur for the methods to ensure largely correct usage (sequence of execution).</remarks>
    public class WorkStateOrchestrator(IWorkStatePersistence persistence, SettingsBase? settings = null, IJsonSerializer? jsonSerializer = null, IIdentifierGenerator? identifierGenerator = null)
    {
        private TimeSpan? _expiryTimeSpan;

        /// <summary>
        /// Gets the <see cref="IWorkStatePersistence"/>.
        /// </summary>
        public IWorkStatePersistence Persistence = persistence.ThrowIfNull(nameof(persistence));

        /// <summary>
        /// Gets the <see cref="IIdentifierGenerator"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Entities.IdentifierGenerator"/></remarks>
        public IIdentifierGenerator IdentifierGenerator = identifierGenerator ?? new IdentifierGenerator();

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="DefaultSettings"/>.</remarks>
        public SettingsBase Settings = settings ?? new DefaultSettings();

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="Json.JsonSerializer.Default"/>.</remarks>
        public IJsonSerializer JsonSerializer = jsonSerializer ?? Json.JsonSerializer.Default;

        /// <summary>
        /// Gets or sets the work expiry <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="SettingsBase.WorkerExpiryTimeSpan"/>.</remarks>
        public TimeSpan ExpiryTimeSpan
        {
            get => _expiryTimeSpan ??= Settings.WorkerExpiryTimeSpan;
            set => _expiryTimeSpan = value;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="WorkState.UserName"/> where not <c>null</c> where performing a <see cref="GetAsync(string, string, CancellationToken)"/> or <see cref="GetAsync{T}(string, CancellationToken)"/>.
        /// </summary>
        public bool CheckUserName { get; set; } = true;

        /// <summary>
        /// Gets the <see cref="WorkState"/> for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="WorkState"/> where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).</remarks>
        public async Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var ws = await Persistence.GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return null;

            // Automatically cancel where expired.
            if (ws.Status != WorkStatus.Finished && DateTimeOffset.UtcNow >= ws.Expiry)
            {
                var wsr = await ExpireAsync(id, "The work has not finished within the expiry timeframe and is assumed to have expired.", cancellationToken).ConfigureAwait(false);
                return wsr.Value;
            }

            return ws;
        }

        /// <summary>
        /// Creates and persists a <b>new</b> <see cref="WorkState"/> with a <see cref="WorkStatus.Created"/> status.
        /// </summary>
        /// <param name="args">The <see cref="WorkStateArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="WorkState"/>.</returns>
        /// <remarks>The <see cref="WorkState.CorrelationId"/> will default to the <see cref="ExecutionContext.CorrelationId"/> where not specified.</remarks>
        public async Task<WorkState> CreateAsync(WorkStateArgs args, CancellationToken cancellationToken = default)
        {
            args.ThrowIfNull(nameof(args));
            var now = DateTimeOffset.UtcNow;
            var ws = new WorkState()
            {
                Id = string.IsNullOrEmpty(args.Id) ? await IdentifierGenerator.GenerateIdentifierAsync<string, WorkStateOrchestrator>().ConfigureAwait(false) : args.Id,
                TypeName = args.TypeName,
                Key = args?.Key,
                CorrelationId = args?.CorrelationId ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current.CorrelationId : Guid.NewGuid().ToString()),
                Status = WorkStatus.Created,
                Created = now,
                Expiry = now.Add(args?.Expiry ?? ExpiryTimeSpan),
                UserName = args?.UserName ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current.UserName : null)
            };

            await Persistence.CreateAsync(ws, cancellationToken).ConfigureAwait(false);

            return ws;
        }

        /// <summary>
        /// Starts a previously <see cref="WorkStatus.Created"/> <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> may also be returned.</remarks>
        public async Task<Result<WorkState>> StartAsync(string id, CancellationToken cancellationToken = default)
        {
            var ws = await Persistence.GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError();

            if (ws.Status != WorkStatus.Created)
                return Result.ConflictError($"Work '{id}' can not be started due to current status of '{ws.Status}'.");

            ws.Status = WorkStatus.Started;
            ws.Started = DateTimeOffset.UtcNow;

            await Persistence.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
            return ws;
        }

        /// <summary>
        /// Sets a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/> to <see cref="WorkStatus.Indeterminate"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="reason">The indeterminate reason.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> may also be returned.</remarks>
        public async Task<Result<WorkState>> IndeterminateAsync(string id, string reason, CancellationToken cancellationToken = default)
        {
            var ws = await GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError();

            if (!WorkStatus.InProgress.HasFlag(ws.Status))
                return Result.ConflictError($"Work '{id}' can not be set to indeterminate due to current status of '{ws.Status}'.");

            ws.Status = WorkStatus.Indeterminate;
            ws.Indeterminate = DateTimeOffset.UtcNow;
            ws.Reason = reason.ThrowIfNullOrEmpty(nameof(reason));

            await Persistence.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
            return ws;
        }

        /// <summary>
        /// Completes a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> may also be returned.</remarks>
        public async Task<Result<WorkState>> CompleteAsync(string id, CancellationToken cancellationToken = default)
        { 
            var ws = await GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError();

            if (!WorkStatus.InProgress.HasFlag(ws.Status))
                return Result.ConflictError($"Work '{id}' can not be completed due to current status of '{ws.Status}'.");

            ws.Status = WorkStatus.Completed;
            ws.Finished = DateTimeOffset.UtcNow;
            ws.Reason = null;

            await Persistence.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
            return ws;
        }

        /// <summary>
        /// Fails a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="reason">The failure reason.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> may also be returned.</remarks>
        public async Task<Result<WorkState>> FailAsync(string id, string reason, CancellationToken cancellationToken = default)
        {
            var ws = await GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError();

            if (!WorkStatus.InProgress.HasFlag(ws.Status))
                return Result.ConflictError($"Work '{id}' can not be failed due to current status of '{ws.Status}'.");

            ws.Status = WorkStatus.Failed;
            ws.Finished = DateTimeOffset.UtcNow;
            ws.Reason = reason.ThrowIfNullOrEmpty(nameof(reason));

            await Persistence.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
            return ws;
        }

        /// <summary>
        /// Fails a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="exception">The unhandled <see cref="Exception"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> may also be returned.</remarks>
        public Task<Result<WorkState>> FailAsync(string id, Exception exception, CancellationToken cancellationToken = default)
            => FailAsync(id, $"Work failed due to an error: {exception.ThrowIfNull(nameof(exception)).Message}", cancellationToken);

        /// <summary>
        /// Expires a <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="reason">The cancellation reason.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Expiring work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
        public async Task<Result<WorkState>> ExpireAsync(string id, string reason, CancellationToken cancellationToken = default)
        {
            var ws = await GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError();

            if (!WorkStatus.Finished.HasFlag(ws.Status))
                return ws;

            ws.Status = WorkStatus.Expired;
            ws.Finished = DateTimeOffset.UtcNow;
            ws.Reason = reason.ThrowIfNullOrEmpty(nameof(reason));

            await Persistence.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
            return ws;
        }

        /// <summary>
        /// Cancels a <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="reason">The cancellation reason.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated <see cref="WorkState"/>.</returns>
        /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Cancelling work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
        public async Task<Result<WorkState>> CancelAsync(string id, string reason, CancellationToken cancellationToken = default)
        {
            var ws = await GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError();

            if (WorkStatus.Finished.HasFlag(ws.Status))
                return Result.Fail($"A cancellation can not be performed when the current status is {ws.Status}.");

            ws.Status = WorkStatus.Cancelled;
            ws.Finished = DateTimeOffset.UtcNow;
            ws.Reason = reason.ThrowIfNullOrEmpty(nameof(reason));

            await Persistence.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
            return ws;
        }

        /// <summary>
        /// Deletes a <see cref="WorkState"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>A <see cref="Result.ConflictError"/> will be returned where attempting to delete work that is not <see cref="WorkStatus.Finished"/>.</remarks>
        public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var ws = await GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.Success;

            if (!WorkStatus.Finished.HasFlag(ws.Status))
                return Result.ConflictError($"Work '{id}' can not be deleted due to current status of '{ws.Status}'; must be considered 'Finished'.");

            await Persistence.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return Result.Success;
        }

        /// <summary>
        /// Gets the result data as <see cref="BinaryData"/> and then JSON deserializes to the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The deserialized value where found; otherwise, <c>default</c>.</returns>
        public async Task<TValue?> GetDataAsync<TValue>(string id, CancellationToken cancellationToken = default)
        {
            var data = await GetDataAsync(id, cancellationToken).ConfigureAwait(false);
            if (data is null)
                return default;

            return JsonSerializer.Deserialize<TValue>(data);
        }

        /// <summary>
        /// Gets the result data as a <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="BinaryData"/> where found; otherwise, <c>null</c>.</returns>
        public Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken = default)
            => Persistence.GetDataAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken);

        /// <summary>
        /// Sets the result data as the specified <paramref name="value"/> serialized as JSON.
        /// </summary>
        /// <typeparam name="TValue">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="id">The work identifier.</param>
        /// <param name="value">The value to JSON serialize as the result data.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task SetDataAsync<TValue>(string id, TValue value, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.SerializeToBinaryData(value.ThrowIfNull(nameof(value)));
            return SetDataAsync(id, json, cancellationToken);
        }

        /// <summary>
        /// Sets the result data as the specified <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="id">The work identifier.</param>
        /// <param name="data">The <see cref="BinaryData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken = default)
        {
            if (await GetAsync(id, cancellationToken).ConfigureAwait(false) is null)
                throw new ArgumentException($"Work '{id}' does not exist.", nameof(id));

            await Persistence.SetDataAsync(id.ThrowIfNullOrEmpty(nameof(id)), data.ThrowIfNull(nameof(data)), cancellationToken).ConfigureAwait(false);
        }

        #region WithType

        /// <summary>
        /// Gets the <see cref="WorkState"/> for the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The <see cref="WorkState.TypeName"/>.</param>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="WorkState"/> where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).
        /// <para>Additionally, the <paramref name="type"/> must equal the <see cref="WorkState.TypeName"/>; and, if <see cref="CheckUserName"/> is <c>true</c> then the <see cref="WorkState.UserName"/> must equal the <see cref="ExecutionContext.UserName"/>
        /// ensuring that the initiating user can only interact with their <see cref="WorkState"/>. Where the aforementioned does not equal then a <c>null</c> will be returned.</para></remarks>
        public async Task<WorkState?> GetAsync(string type, string id, CancellationToken cancellationToken = default)
        {
            var ws = await GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (ws is null || ws.TypeName != type)
                return null;

            if (CheckUserName && ws.UserName is not null && ws.UserName != (ExecutionContext.HasCurrent ? ExecutionContext.Current.UserName : null))
                return null;

            return ws;
        }

        /// <summary>
        /// Gets the <see cref="WorkState"/> for the specified <paramref name="id"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
        /// <param name="id">The work identifier.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="WorkState"/> where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).
        /// <para>Additionally, the <typeparamref name="T"/> must equal the <see cref="WorkState.TypeName"/>; and, if <see cref="CheckUserName"/> is <c>true</c> then the <see cref="WorkState.UserName"/> must equal the <see cref="ExecutionContext.UserName"/>
        /// ensuring that the initiating user can only interact with their <see cref="WorkState"/>. Where the aforementioned does not equal then a <c>null</c> will be returned.</para></remarks>
        public Task<WorkState?> GetAsync<T>(string id, CancellationToken cancellationToken = default) => GetAsync(WorkStateArgs.GetTypeName<T>(), id, cancellationToken);

        #endregion
    }
}