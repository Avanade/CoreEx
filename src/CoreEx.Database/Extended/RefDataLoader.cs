// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
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
    public class RefDataLoader<TColl, TItem, TId>
        where TColl : class, IReferenceDataCollection<TId, TItem>, new()
        where TItem : class, IReferenceData<TId>, new()
        where TId : IComparable<TId>, IEquatable<TId>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefDataLoader{TColl, TItem, TId}"/> class.
        /// </summary>
        /// <param name="command">The <see cref="DatabaseCommand"/>.</param>
        public RefDataLoader(DatabaseCommand command) => Command = command ?? throw new ArgumentNullException(nameof(command));

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        public DatabaseCommand Command { get; }

        /// <summary>
        /// Executes a dynamic <see cref="IReferenceDataCollection"/> query updating the <paramref name="coll"/>.
        /// </summary>
        /// <param name="coll">The <see cref="IReferenceDataCollection"/>.</param>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name override; defaults to <see cref="Extended.DatabaseColumns.RefDataIdName"/>.</param>
        /// <param name="additionalProperties">The additional properties action that enables non-standard properties to be updated from the <see cref="DatabaseRecord"/>.</param>
        /// <param name="multiSetArgs">The additional <see cref="IMultiSetArgs"/> where additional datasets are returned.</param>
        /// <param name="confirmItemIsToBeAdded">The action to confirm whether the item is to be added (defaults to <c>true</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public Task LoadAsync(TColl coll, string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null, IEnumerable<IMultiSetArgs>? multiSetArgs = null,
            Func<DatabaseRecord, TItem, bool>? confirmItemIsToBeAdded = null, CancellationToken cancellationToken = default)
        {
            if (coll == null)
                throw new ArgumentNullException(nameof(coll));

            var list = new List<IMultiSetArgs> { new RefDataMultiSetCollArgs<TColl, TItem, TId>(Command.Database, r => coll.Add(r), idColumnName, additionalProperties, confirmItemIsToBeAdded) };
            if (multiSetArgs != null)
                list.AddRange(multiSetArgs);

            return Command.SelectMultiSetAsync(list, cancellationToken);
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
        {
            var coll = new TColl();
            await LoadAsync(coll, idColumnName, additionalProperties, multiSetArgs, confirmItemIsToBeAdded, cancellationToken).ConfigureAwait(false);
            return coll;
        }
    }
}