// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using CoreEx.Results;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Provides dynamic <see cref="IReferenceDataCollection"/> loading capabilities.
    /// </summary>
    /// <typeparam name="TColl">The <see cref="IReferenceDataCollection"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IReferenceData"/> item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
    public class RefDataLoader<TColl, TItem, TId>(DatabaseCommand command)
        where TColl : class, IReferenceDataCollection<TId, TItem>, new()
        where TItem : class, IReferenceData<TId>, new()
        where TId : IComparable<TId>, IEquatable<TId>
    {

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        public DatabaseCommand Command { get; } = command.ThrowIfNull(nameof(command));

        /// <summary>
        /// Executes a dynamic <see cref="IReferenceDataCollection"/> query updating the <paramref name="coll"/>.
        /// </summary>
        /// <param name="coll">The <see cref="IReferenceDataCollection"/>.</param>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name override; defaults to <see cref="Extended.DatabaseColumns.RefDataIdName"/>.</param>
        /// <param name="additionalProperties">The additional properties action that enables non-standard properties to be updated from the <see cref="DatabaseRecord"/>.</param>
        /// <param name="multiSetArgs">The additional <see cref="IMultiSetArgs"/> where additional datasets are returned.</param>
        /// <param name="confirmItemIsToBeAdded">The action to confirm whether the item is to be added (defaults to <c>true</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task LoadAsync(TColl coll, string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null, IEnumerable<IMultiSetArgs>? multiSetArgs = null,
            Func<DatabaseRecord, TItem, bool>? confirmItemIsToBeAdded = null, CancellationToken cancellationToken = default)
            => (await LoadWithResultAsync(coll, idColumnName, additionalProperties, multiSetArgs, confirmItemIsToBeAdded, cancellationToken)).ThrowOnError();

        /// <summary>
        /// Executes a dynamic <see cref="IReferenceDataCollection"/> query updating the <paramref name="coll"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="coll">The <see cref="IReferenceDataCollection"/>.</param>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name override; defaults to <see cref="Extended.DatabaseColumns.RefDataIdName"/>.</param>
        /// <param name="additionalProperties">The additional properties action that enables non-standard properties to be updated from the <see cref="DatabaseRecord"/>.</param>
        /// <param name="multiSetArgs">The additional <see cref="IMultiSetArgs"/> where additional datasets are returned.</param>
        /// <param name="confirmItemIsToBeAdded">The action to confirm whether the item is to be added (defaults to <c>true</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public async Task<Result> LoadWithResultAsync(TColl coll, string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null, IEnumerable<IMultiSetArgs>? multiSetArgs = null,
            Func<DatabaseRecord, TItem, bool>? confirmItemIsToBeAdded = null, CancellationToken cancellationToken = default)
        {
            coll.ThrowIfNull(nameof(coll));

            var list = new List<IMultiSetArgs> { new RefDataMultiSetCollArgs<TColl, TItem, TId>(Command.Database, coll.Add, idColumnName, additionalProperties, confirmItemIsToBeAdded) };
            if (multiSetArgs != null)
                list.AddRange(multiSetArgs);

            var result = await Command.SelectMultiSetWithResultAsync(list, cancellationToken).ConfigureAwait(false);
            return result.Then(() => Cleaner.Clean(coll));
        }

        /// <summary>
        /// Executes a dynamic <see cref="IReferenceDataCollection"/> query.
        /// </summary>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name override; defaults to <see cref="Extended.DatabaseColumns.RefDataIdName"/>.</param>
        /// <param name="additionalProperties">The additional properties action that enables non-standard properties to be updated from the <see cref="DatabaseRecord"/>.</param>
        /// <param name="multiSetArgs">The additional <see cref="IMultiSetArgs"/> where additional datasets are returned.</param>
        /// <param name="confirmItemIsToBeAdded">The action to confirm whether the item is to be added (defaults to <c>true</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IReferenceDataCollection"/>.</returns>
        public async Task<TColl> LoadAsync(string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null, IEnumerable<IMultiSetArgs>? multiSetArgs = null,
            Func<DatabaseRecord, TItem, bool>? confirmItemIsToBeAdded = null, CancellationToken cancellationToken = default)
            => (await LoadWithResultAsync(idColumnName, additionalProperties, multiSetArgs, confirmItemIsToBeAdded, cancellationToken)).ThrowOnError();

        /// <summary>
        /// Executes a dynamic <see cref="IReferenceDataCollection"/> query with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name override; defaults to <see cref="Extended.DatabaseColumns.RefDataIdName"/>.</param>
        /// <param name="additionalProperties">The additional properties action that enables non-standard properties to be updated from the <see cref="DatabaseRecord"/>.</param>
        /// <param name="multiSetArgs">The additional <see cref="IMultiSetArgs"/> where additional datasets are returned.</param>
        /// <param name="confirmItemIsToBeAdded">The action to confirm whether the item is to be added (defaults to <c>true</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IReferenceDataCollection"/>.</returns>
        public async Task<Result<TColl>> LoadWithResultAsync(string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null, IEnumerable<IMultiSetArgs>? multiSetArgs = null,
            Func<DatabaseRecord, TItem, bool>? confirmItemIsToBeAdded = null, CancellationToken cancellationToken = default)
        {
            var coll = new TColl();
            var result = await LoadWithResultAsync(coll, idColumnName, additionalProperties, multiSetArgs, confirmItemIsToBeAdded, cancellationToken).ConfigureAwait(false);
            return result.ThenAs(() => coll);
        }
    }
}