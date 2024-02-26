// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.RefData;
using System;
using System.Collections.Generic;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Represents a dynamic <see cref="IReferenceData"/> query-only mapper.
    /// </summary>
    /// <typeparam name="TItem">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TId">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    internal class RefDataMapper<TItem, TId> : DatabaseQueryMapper<TItem> where TItem : class, IReferenceData<TId>, new() where TId : IComparable<TId>, IEquatable<TId>
    {
        private readonly DatabaseColumns _cols;
        private readonly string _idCol;
        private readonly Action<DatabaseRecord, TItem>? _additionalProperties;
        private Dictionary<string, int>? _fields;

        /// <summary>
        /// Initializes a new instanace of the <see cref="RefDataMapper{TItem, TId}"/>.
        /// </summary>
        /// <param name="database">The <see cref="IDatabase"/>.</param>
        /// <param name="idColumnName">The <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> column name.</param>
        /// <param name="additionalProperties"></param>
        public RefDataMapper(IDatabase database, string? idColumnName = null, Action<DatabaseRecord, TItem>? additionalProperties = null)
        {
            _cols = database.DatabaseColumns;
            _idCol = idColumnName ?? _cols.RefDataIdName;
            _additionalProperties = additionalProperties;
        }

        /// <inheritdoc/>
        public override TItem MapFromDb(DatabaseRecord dr, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (_fields == null)
            {
                _fields = [];
                for (var i = 0; i < dr.DataReader.FieldCount; i++)
                {
                    _fields.Add(dr.DataReader.GetName(i), i);
                }

                if (!_fields.ContainsKey(_idCol) || !_fields.ContainsKey(_cols.RefDataCodeName))
                    throw new InvalidOperationException($"The reference data query must return as a minimum the Id and Code columns as per the configured names.");
            }

            var item = new TItem()
            {
                Id = dr.GetValue<TId>(_idCol),
                Code = dr.GetValue<string?>(_cols.RefDataCodeName),
                Text = _fields.ContainsKey(_cols.RefDataTextName) ? dr.GetValue<string?>(_cols.RefDataTextName) : null,
                Description = _fields.ContainsKey(_cols.RefDataDescriptionName) ? dr.GetValue<string?>(_cols.RefDataDescriptionName) : null,
                SortOrder = _fields.ContainsKey(_cols.RefDataSortOrderName) ? dr.GetValue<int>(_cols.RefDataSortOrderName) : 0,
                IsActive = _fields.ContainsKey(_cols.RefDataIsActiveName) && dr.GetValue<bool>(_cols.RefDataIsActiveName),
                StartDate = _fields.ContainsKey(_cols.RefDataStartDateName) ? dr.GetValue<DateTime?>(_cols.RefDataStartDateName) : null,
                EndDate = _fields.ContainsKey(_cols.RefDataEndDateName) ? dr.GetValue<DateTime?>(_cols.RefDataEndDateName) : null,
                ETag = _fields.ContainsKey(_cols.ETagName) ? dr.GetRowVersion(_cols.ETagName) : null
            };

            _additionalProperties?.Invoke(dr, item);

            return item;
        }
    }
}