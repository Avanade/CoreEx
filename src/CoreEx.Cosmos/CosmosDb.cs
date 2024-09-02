// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Cosmos.Model;
using CoreEx.Cosmos.Extended;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides extended <b>CosmosDb</b> data access.
    /// </summary>
    /// <param name="database">The <see cref="Microsoft.Azure.Cosmos.Database"/>.</param>
    /// <param name="mapper">The <see cref="IMapper"/>.</param>
    /// <param name="invoker">Enables the <see cref="Invoker"/> to be overridden; defaults to <see cref="CosmosDbInvoker"/>.</param>
    public class CosmosDb(Database database, IMapper mapper, CosmosDbInvoker? invoker = null) : ICosmosDb
    {
        private static CosmosDbInvoker? _invoker;
        private readonly ConcurrentDictionary<Key, Func<IQueryable, IQueryable>> _filters = new();

        /// <summary>
        /// Provides key as combination of model type and container identifier.
        /// </summary>
        private readonly struct Key(Type modelType, string containerId)
        {
            public Type ModelType { get; } = modelType;

            public string ContainerId { get; } = containerId;
        }

        /// <inheritdoc/>
        public Database Database { get; } = database.ThrowIfNull(nameof(database));

        /// <inheritdoc/>
        public IMapper Mapper { get; } = mapper.ThrowIfNull(nameof(mapper));

        /// <inheritdoc/>
        public CosmosDbInvoker Invoker { get; } = invoker ?? (_invoker ??= new CosmosDbInvoker());

        /// <inheritdoc/>
        public CosmosDbArgs DbArgs { get; set; } = new CosmosDbArgs();

        /// <inheritdoc/>
        public Container GetCosmosContainer(string containerId) => Database.GetContainer(containerId);

        /// <inheritdoc/>
        public CosmosDbContainer<T, TModel> Container<T, TModel>(string containerId, CosmosDbArgs? dbArgs = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <inheritdoc/>
        public CosmosDbValueContainer<T, TModel> ValueContainer<T, TModel>(string containerId, CosmosDbArgs? dbArgs = null) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <inheritdoc/>
        public CosmosDbModelContainer<TModel> ModelContainer<TModel>(string containerId, CosmosDbArgs? dbArgs = null) where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <inheritdoc/>
        public CosmosDbValueModelContainer<TModel> ValueModelContainer<TModel>(string containerId, CosmosDbArgs? dbArgs = null) where TModel : class, IEntityKey, new() => new(this, containerId, dbArgs);

        /// <summary>
        /// Sets the filter for all operations performed on the <typeparamref name="TModel"/> for the specified <paramref name="containerId"/> to ensure authorisation is applied. Applies automatically 
        /// to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <typeparam name="TModel">The model <see cref="Type"/> persisted within the container.</typeparam>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <param name="filter">The filter query.</param>
        /// <remarks>The <see cref="CosmosDb"/> instance to support fluent-style method-chaining.</remarks>
        public CosmosDb UseAuthorizeFilter<TModel>(string containerId, Func<IQueryable, IQueryable> filter)
        {
            if (!_filters.TryAdd(new Key(typeof(TModel), containerId.ThrowIfNull(nameof(containerId))), filter.ThrowIfNull(nameof(filter))))
                throw new InvalidOperationException("A filter cannot be overridden.");

            return this;
        }

        /// <inheritdoc/>
        public Func<IQueryable, IQueryable>? GetAuthorizeFilter<TModel>(string containerId) => _filters.TryGetValue(new Key(typeof(TModel), containerId.ThrowIfNull(nameof(containerId))), out var filter) ? filter : null;

        /// <inheritdoc/>
        public Result? HandleCosmosException(CosmosException cex) => OnCosmosException(cex);

        /// <summary>
        /// Provides the <see cref="CosmosException"/> handling as a result of <see cref="HandleCosmosException(CosmosException)"/>.
        /// </summary>
        /// <param name="cex">The <see cref="CosmosException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/>.</returns>
        /// <remarks>Where overridding and the <see cref="CosmosException"/> is not specifically handled then invoke the base to ensure any standard handling is executed.</remarks>
        protected virtual Result? OnCosmosException(CosmosException cex) => cex.ThrowIfNull(nameof(cex)).StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => Result.Fail(new NotFoundException(null, cex)),
            System.Net.HttpStatusCode.Conflict => Result.Fail(new DuplicateException(null, cex)),
            System.Net.HttpStatusCode.PreconditionFailed => Result.Fail(new ConcurrencyException(null, cex)),
            _ => Result.Fail(cex)
        };

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectMultiSetAsync(PartitionKey partitionKey, params IMultiSetArgs[] multiSetArgs) => SelectMultiSetAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectMultiSetAsync(PartitionKey partitionKey, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectMultiSetAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetArgs}, CancellationToken)"/> for further details.</remarks>
        public async Task SelectMultiSetAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => (await SelectMultiSetWithResultAsync(partitionKey, sql, multiSetArgs, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(PartitionKey partitionKey, params IMultiSetArgs[] multiSetArgs) => SelectMultiSetWithResultAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectMultiSetWithResultAsync(PartitionKey partitionKey, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectMultiSetWithResultAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <paramref name="multiSetArgs"/> must all be from the same <see cref="CosmosDb"/>, be of type <see cref="CosmosDbValueContainer{T, TModel}"/>, and reference the same <see cref="Container.Id"/>. Each 
        /// <paramref name="multiSetArgs"/> is verified and executed in the order specified.
        /// <para>The underlying SQL will be automatically created from the specified <paramref name="multiSetArgs"/> where not explicitly supplied. Essentially, it is a simple query where all <i>types</i> inferred from the <paramref name="multiSetArgs"/>
        /// are included, for example: <c>SELECT * FROM c WHERE c.type in ("TypeNameA", "TypeNameB")</c></para>
        /// <para>Example usage is:
        /// <code>
        /// private async Task&lt;Result&lt;MemberDetail?&gt;&gt; GetDetailOnImplementationAsync(int id)
        /// {
        ///     MemberDetail? md = null;
        ///     return await Result.GoAsync(() =&gt; _cosmos.SelectMultiSetWithResultAsync(new AzCosmos.PartitionKey(id.ToString()),
        ///             _cosmos.Members.CreateMultiSetSingleArgs(m =&gt; md = m.CreateCopyFromAs&lt;MemberDetail&gt;(), isMandatory: false, stopOnNull: true),
        ///             _cosmos.MemberAddresses.CreateMultiSetCollArgs(mac =&gt; md.Adjust(x =&gt; x.Addresses = new (mac))))) 
        ///         .ThenAs(() =&gt; md).ConfigureAwait(false);
        /// }
        /// </code></para></remarks>
        public async Task<Result> SelectMultiSetWithResultAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            // Verify that the multi set arguments are valid for this type of get query.
            var multiSetList = multiSetArgs?.ToArray() ?? null;
            if (multiSetList == null || multiSetList.Length == 0)
                throw new ArgumentException($"At least one {nameof(IMultiSetArgs)} must be supplied.", nameof(multiSetArgs));

            if (multiSetList.Any(x => x.Container.CosmosDb != this))
                throw new ArgumentException($"All {nameof(IMultiSetArgs)} containers must be from this same database.", nameof(multiSetArgs));

            if (multiSetList.Any(x => !x.Container.IsCosmosDbValueModel))
                throw new ArgumentException($"All {nameof(IMultiSetArgs)} containers must be of type CosmosDbValueContainer.", nameof(multiSetArgs));

            var container = multiSetList[0].Container;
            var types = new Dictionary<string, IMultiSetArgs>([ new KeyValuePair<string, IMultiSetArgs>(container.ModelType.Name, multiSetList[0]) ]);
            var sb = string.IsNullOrEmpty(sql) ? new StringBuilder($"SELECT * FROM c WHERE c.type in (\"{container.ModelType.Name}\"") : null; 

            for (int i = 1; i < multiSetList.Length; i++)
            {
                if (multiSetList[i].Container.Container.Id != container.Container.Id)
                    throw new ArgumentException($"All {nameof(IMultiSetArgs)} containers must reference the same container id.", nameof(multiSetArgs));

                if (!types.TryAdd(multiSetList[i].Container.ModelType.Name, multiSetList[i]))
                    throw new ArgumentException($"All {nameof(IMultiSetArgs)} containers must be of different model type.", nameof(multiSetArgs));

                sb?.Append($", \"{multiSetList[i].Container.ModelType.Name}\"");
            }

            sb?.Append(')');

            // Execute the Cosmos DB query.
            var result = await Invoker.InvokeAsync(this, container, sb?.ToString() ?? sql, types, async (_, container, sql, types, ct) =>
            {
                // Set up for work.
                var da = new CosmosDbArgs(container.DbArgs, partitionKey);
                var qsi = container.Container.GetItemQueryStreamIterator(sql, requestOptions: da.GetQueryRequestOptions());
                IJsonSerializer js = ExecutionContext.GetService<IJsonSerializer>() ?? CoreEx.Json.JsonSerializer.Default;
                var isStj = js is Text.Json.JsonSerializer;

                while (qsi.HasMoreResults)
                {
                    var rm = await qsi.ReadNextAsync(ct).ConfigureAwait(false);
                    if (!rm.IsSuccessStatusCode)
                        return Result.Fail(new InvalidOperationException(rm.ErrorMessage));

                    var json = JsonDocument.Parse(rm.Content);
                    if (!json.RootElement.TryGetProperty("Documents", out var jds) || jds.ValueKind != JsonValueKind.Array)
                        return Result.Fail(new InvalidOperationException("Cosmos response JSON 'Documents' property either not found in result or is not an array."));

                    foreach (var jd in jds.EnumerateArray())
                    {
                        if (!jd.TryGetProperty("type", out var jt) || jt.ValueKind != JsonValueKind.String)
                            return Result.Fail(new InvalidOperationException("Cosmos response documents item 'type' property either not found in result or is not a string."));

                        if (!types.TryGetValue(jt.GetString()!, out var msa))
                            continue;   // Ignore any unexpected type.

                        var model = isStj 
                            ? jd.Deserialize(msa.Container.ModelValueType, (JsonSerializerOptions)js.Options) 
                            : js.Deserialize(jd.ToString(), msa.Container.ModelValueType);

                        if (!msa.Container.IsModelValid(model, msa.Container.DbArgs, true))
                            continue;

                        var result = msa.AddItem(msa.Container.MapToValue(model));
                        if (result.IsFailure)
                            return result;
                    }
                }

                return Result.Success;
            }, cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
                return result;

            // Validate the multi-set args and action each accordingly.
            foreach (var msa in multiSetList)
            {
                var r = msa.Verify();
                if (r.IsFailure)
                    return r.AsResult();

                if (!r.Value && msa.StopOnNull)
                    break;

                msa.Invoke();
            }

            return Result.Success;
        }
    }
}