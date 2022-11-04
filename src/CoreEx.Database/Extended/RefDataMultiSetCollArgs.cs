// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using System;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Provides the <see cref="IReferenceData"/> <see cref="MultiSetCollArgs"/>.
    /// </summary>
    /// <typeparam name="TColl">The <see cref="IReferenceDataCollection"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IReferenceData"/> item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    internal class RefDataMultiSetCollArgs<TColl, TItem, TId> : MultiSetCollArgs
        where TColl : class, IReferenceDataCollection<TId, TItem>
        where TItem : class, IReferenceData<TId>, new()
        where TId : IComparable<TId>, IEquatable<TId>
    {
        private readonly Action<TItem> _item;
        private readonly RefDataMapper<TItem, TId> _refDataMapper;
        private readonly Action<DatabaseRecord, TItem>? _additionalProperties;
        private readonly Func<DatabaseRecord, TItem, bool>? _confirmItemIsToBeAdded;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefDataMultiSetCollArgs{TColl, TItem, TId}"/> class. 
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="item">The action that will be invoked with the result of each item.</param>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name override; defaults to <see cref="Extended.DatabaseColumns.RefDataIdName"/>.</param>
        /// <param name="additionalProperties">The additional properties action that enables non-standard properties to be updated from the <see cref="DatabaseRecord"/>.</param>
        /// <param name="confirmItemIsToBeAdded">The action to confirm whether the item is to be added (defaults to <c>true</c>).</param>
        public RefDataMultiSetCollArgs(IDatabase database, Action<TItem> item, string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null, Func<DatabaseRecord, TItem, bool>? confirmItemIsToBeAdded = null)
            : base(stopOnNull: true)
        {
            _item = item;
            _refDataMapper = new RefDataMapper<TItem, TId>(database, idColumnName, additionalProperties);
            _additionalProperties = additionalProperties;
            _confirmItemIsToBeAdded = confirmItemIsToBeAdded;
        }

        /// <inheritdoc/>
        public override void DatasetRecord(DatabaseRecord dr)
        {
            if (dr == null)
                throw new ArgumentNullException(nameof(dr));

            var rdi = _refDataMapper.MapFromDb(dr);
            if (_confirmItemIsToBeAdded == null || _confirmItemIsToBeAdded(dr, rdi))
                _item(rdi);
        }
    }
}