// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Stj = System.Text.Json;

namespace CoreEx.Cosmos.Model
{
    /// <summary>
    /// Provides a typed interface for the primary <see cref="CosmosDbContainer"/> <see cref="CosmosDbValueModelQuery{TModel}"/> operations.
    /// </summary>
    /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
    public sealed class CosmosDbValueModelContainer<TModel> where TModel : class, IEntityKey, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbValueModelContainer{TModel}"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="CosmosDbContainer"/>.</param>
        internal CosmosDbValueModelContainer(CosmosDbContainer owner) => Owner = owner.ThrowIfNull(nameof(owner)); 

        /// <summary>
        /// Gets the owning <see cref="CosmosDbContainer"/>.
        /// </summary>
        public CosmosDbContainer Owner { get; }

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.Container"/>.
        /// </summary>
        public Container CosmosContainer => Owner.CosmosContainer;

        /// <summary>
        /// Sets the function to get the <see cref="PartitionKey"/> from the <see cref="CosmosDbValue{TModel}"/> used by the <see cref="CosmosDbModelContainer.GetPartitionKey{TModel}(CosmosDbValue{TModel}, CosmosDbArgs)"/> (used by only by the <b>Create</b> and <b>Update</b> operations).
        /// </summary>
        /// <param name="getPartitionKey">The function to get the <see cref="PartitionKey"/> from the model.</param>
        /// <remarks>This can only be set once; otherwise, a <see cref="InvalidOperationException"/> will be thrown.</remarks>
        public CosmosDbValueModelContainer<TModel> UsePartitionKey(Func<CosmosDbValue<TModel>, PartitionKey>? getPartitionKey)
        {
            Owner.Model.UsePartitionKey(getPartitionKey);
            return this;
        }

        /// <summary>
        /// Sets (overrides) the name for the model <see cref="Type"/>.
        /// </summary>
        /// <param name="name">The model name.</param>
        public CosmosDbValueModelContainer<TModel> UseModelName(string name)
        {
            Owner.Model.UseModelName<TModel>(name);
            return this;
        }

        /// <summary>
        /// Sets the filter for all operations performed on the <see cref="CosmosDbValue{TModel}"/> to ensure authorisation is applied. Applies automatically to all queries, plus create, update, delete and get operations.
        /// </summary>
        /// <param name="filter">The authorization filter query.</param>
        public CosmosDbContainer UseAuthorizeFilter(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? filter)
        {
            Owner.Model.UseAuthorizeFilter(filter);
            return Owner;
        }

        #region Query

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueModelQuery{TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> Query(Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query) => Owner.Model.ValueQuery<TModel>(query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueModelQuery{TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> Query(PartitionKey? partitionKey = null, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => Owner.Model.ValueQuery<TModel>(partitionKey, query);

        /// <summary>
        /// Gets (creates) a <see cref="CosmosDbValueModelQuery{TModel}"/> to enable LINQ-style queries.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="query">The function to perform additional query execution.</param>
        /// <returns>The <see cref="CosmosDbValueModelQuery{TModel}"/>.</returns>
        public CosmosDbValueModelQuery<TModel> Query(CosmosDbArgs dbArgs, Func<IQueryable<CosmosDbValue<TModel>>, IQueryable<CosmosDbValue<TModel>>>? query = null) => Owner.Model.ValueQuery<TModel>(dbArgs, query);

        #endregion

        #region Get

        /// <summary>
        /// Gets the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<CosmosDbValue<TModel>?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetValueAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Gets the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetValueWithResultAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Gets the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<CosmosDbValue<TModel>?> GetAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.GetValueAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Gets the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.GetValueWithResultAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Gets the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<CosmosDbValue<TModel>?> GetAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetValueAsync<TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Gets the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c> (see <see cref="CosmosDbArgs.NullOnNotFound"/>).</returns>
        public Task<Result<CosmosDbValue<TModel>?>> GetWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.GetValueWithResultAsync<TModel>(dbArgs, key, cancellationToken);

        #endregion

        #region Create

        /// <summary>
        /// Creates the model (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<CosmosDbValue<TModel>> CreateAsync(CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.CreateValueAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Creates the model (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<CosmosDbValue<TModel>>> CreateWithResultAsync(CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.CreateValueWithResultAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Creates the model (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<CosmosDbValue<TModel>> CreateAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.CreateValueAsync<TModel>(dbArgs, value, cancellationToken);

        /// <summary>
        /// Creates the model (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The created value.</returns>
        public Task<Result<CosmosDbValue<TModel>>> CreateWithResultAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.CreateValueWithResultAsync<TModel>(dbArgs, value, cancellationToken);

        #endregion

        #region Update

        /// <summary>
        /// Updates the model (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<CosmosDbValue<TModel>> UpdateAsync(CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.UpdateValueAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Updates the model (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<CosmosDbValue<TModel>>> UpdateWithResultAsync(CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.UpdateValueWithResultAsync<TModel>(value, cancellationToken);

        /// <summary>
        /// Updates the model (using underlying <see cref="CosmosDbValue{TModel}"/>).
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<CosmosDbValue<TModel>> UpdateAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.UpdateValueAsync<TModel>(dbArgs, value, cancellationToken);

        /// <summary>
        /// Updates the model (using underlying <see cref="CosmosDbValue{TModel}"/>) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <param name="value">The value to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The updated value.</returns>
        public Task<Result<CosmosDbValue<TModel>>> UpdateWithResultAsync(CosmosDbArgs dbArgs, CosmosDbValue<TModel> value, CancellationToken cancellationToken = default) => Owner.Model.UpdateValueWithResultAsync<TModel>(dbArgs, value, cancellationToken);

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteValueAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Deletes the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteValueWithResultAsync<TModel>(key, cancellationToken);

        /// <summary>
        /// Deletes the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.DeleteValueAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Deletes the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>. Defaults to <see cref="CosmosDbContainer.DbArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CompositeKey key, PartitionKey? partitionKey, CancellationToken cancellationToken = default) => Owner.Model.DeleteValueWithResultAsync<TModel>(key, partitionKey, cancellationToken);

        /// <summary>
        /// Deletes the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task DeleteAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteValueAsync<TModel>(dbArgs, key, cancellationToken);

        /// <summary>
        /// Deletes the model (using underlying <see cref="CosmosDbValue{TModel}"/>) for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>..</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task<Result> DeleteWithResultAsync(CosmosDbArgs dbArgs, CompositeKey key, CancellationToken cancellationToken = default) => Owner.Model.DeleteValueWithResultAsync<TModel>(dbArgs, key, cancellationToken);

        #endregion

        #region MultiSet

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectValueMultiSetAsync(PartitionKey partitionKey, params IMultiSetValueArgs[] multiSetArgs) => SelectValueMultiSetAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task SelectValueMultiSetAsync(PartitionKey partitionKey, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectValueMultiSetAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public async Task SelectValueMultiSetAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default)
            => (await SelectValueMultiSetWithResultAsync(partitionKey, sql, multiSetArgs, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectValueMultiSetWithResultAsync(PartitionKey partitionKey, params IMultiSetValueArgs[] multiSetArgs) => SelectValueMultiSetWithResultAsync(partitionKey, multiSetArgs, default);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>See <see cref="SelectValueMultiSetWithResultAsync(PartitionKey, string?, IEnumerable{IMultiSetValueArgs}, CancellationToken)"/> for further details.</remarks>
        public Task<Result> SelectValueMultiSetWithResultAsync(PartitionKey partitionKey, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default) => SelectValueMultiSetWithResultAsync(partitionKey, null, multiSetArgs, cancellationToken);

        /// <summary>
        /// Executes a multi-dataset query command with one or more <see cref="IMultiSetValueArgs"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="partitionKey">The <see cref="PartitionKey"/>.</param>
        /// <param name="sql">The override SQL statement; will default where not specified.</param>
        /// <param name="multiSetArgs">One or more <see cref="IMultiSetValueArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>The <paramref name="multiSetArgs"/> must be of type <see cref="CosmosDbValue{TModel}"/>. Each <paramref name="multiSetArgs"/> is verified and executed in the order specified.
        /// <para>The underlying SQL will be automatically created from the specified <paramref name="multiSetArgs"/> where not explicitly supplied. Essentially, it is a simple query where all <i>types</i> inferred from the <paramref name="multiSetArgs"/>
        /// are included, for example: <c>SELECT * FROM c WHERE c.type in ("TypeNameA", "TypeNameB")</c></para>
        /// </remarks>
        public async Task<Result> SelectValueMultiSetWithResultAsync(PartitionKey partitionKey, string? sql, IEnumerable<IMultiSetValueArgs> multiSetArgs, CancellationToken cancellationToken = default)
        {
            // Verify that the multi set arguments are valid for this type of get query.
            var multiSetList = multiSetArgs?.ToArray() ?? null;
            if (multiSetList == null || multiSetList.Length == 0)
                throw new ArgumentException($"At least one {nameof(IMultiSetValueArgs)} must be supplied.", nameof(multiSetArgs));

            // Build the Cosmos SQL statement.
            var name = multiSetList[0].GetModelName(Owner);
            var types = new Dictionary<string, IMultiSetValueArgs>([new KeyValuePair<string, IMultiSetValueArgs>(name, multiSetList[0])]);
            var sb = string.IsNullOrEmpty(sql) ? new StringBuilder($"SELECT * FROM c WHERE c.type in (\"{name}\"") : null;

            for (int i = 1; i < multiSetList.Length; i++)
            {
                name = multiSetList[i].GetModelName(Owner);
                if (!types.TryAdd(name, multiSetList[i]))
                    throw new ArgumentException($"All {nameof(IMultiSetValueArgs)} must be of different model type.", nameof(multiSetArgs));

                sb?.Append($", \"{name}\"");
            }

            sb?.Append(')');

            // Execute the Cosmos DB query.
            var result = await Owner.CosmosDb.Invoker.InvokeAsync(Owner.CosmosDb, Owner, sb?.ToString() ?? sql, types, async (_, container, sql, types, ct) =>
            {
                // Set up for work.
                var da = new CosmosDbArgs(container.DbArgs, partitionKey);
                var qsi = CosmosContainer.GetItemQueryStreamIterator(sql, requestOptions: da.GetQueryRequestOptions());
                IJsonSerializer js = ExecutionContext.GetService<IJsonSerializer>() ?? CoreEx.Json.JsonSerializer.Default;
                var isStj = js is Text.Json.JsonSerializer;

                while (qsi.HasMoreResults)
                {
                    var rm = await qsi.ReadNextAsync(ct).ConfigureAwait(false);
                    if (!rm.IsSuccessStatusCode)
                        return Result.Fail(new InvalidOperationException(rm.ErrorMessage));

                    var json = Stj.JsonDocument.Parse(rm.Content);
                    if (!json.RootElement.TryGetProperty("Documents", out var jds) || jds.ValueKind != Stj.JsonValueKind.Array)
                        return Result.Fail(new InvalidOperationException("Cosmos response JSON 'Documents' property either not found in result or is not an array."));

                    foreach (var jd in jds.EnumerateArray())
                    {
                        if (!jd.TryGetProperty("type", out var jt) || jt.ValueKind != Stj.JsonValueKind.String)
                            return Result.Fail(new InvalidOperationException("Cosmos response documents item 'type' property either not found in result or is not a string."));

                        if (!types.TryGetValue(jt.GetString()!, out var msa))
                            continue;   // Ignore any unexpected type.

                        var model = isStj
                            ? jd.Deserialize(msa.Type, (Stj.JsonSerializerOptions)js.Options)
                            : js.Deserialize(jd.ToString(), msa.Type);

                        if (model is null)
                            return Result.Fail(new InvalidOperationException($"Cosmos response documents item type '{jt.GetRawText()}' deserialization resulted in a null."));

                        var result = msa.AddItem(Owner, da, model);
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

        #endregion
    }
}