// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents a base <see cref="IReferenceDataCollection{TId, TRef}"/> collection.
    /// </summary>
    /// <typeparam name="TId">The <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    public class ReferenceDataCollection<TId, TRef> : IReferenceDataCollection<TId, TRef>, ICollection<TRef> where TId : IComparable<TId>, IEquatable<TId> where TRef : class, IReferenceData<TId>
    {
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<TId, TRef> _rdcId = new();
        private readonly ConcurrentDictionary<string, TRef> _rdcCode;
        private Dictionary<(string, object?), TRef>? _mappingsDict;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCollection{TItem, TId}"/> class.
        /// </summary>
        /// <param name="sortOrder">The <see cref="ReferenceDataSortOrder"/>. Defaults to <see cref="ReferenceDataSortOrder.SortOrder"/>.</param>
        /// <param name="codeComparer">The <see cref="StringComparer"/> for <see cref="IReferenceData.Code"/> comparisons. Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
        public ReferenceDataCollection(ReferenceDataSortOrder sortOrder = ReferenceDataSortOrder.SortOrder, StringComparer? codeComparer = null)
        {
            SortOrder = sortOrder;
            _rdcCode = new ConcurrentDictionary<string, TRef>(codeComparer ?? StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the <see cref="ReferenceDataSortOrder"/> used by <see cref="GetList"/>.
        /// </summary>
        public ReferenceDataSortOrder SortOrder { get; set; }

        /// <summary>
        /// Gets the item for the specified <see cref="IReferenceData.Code"/>.
        /// </summary>
        /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        public TRef? this[string code] => _rdcCode[code];

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_lock)
            {
                _rdcId.Clear();
                _rdcCode.Clear();
                _mappingsDict?.Clear();
            }
        }

        /// <inheritdoc/>
        /// <remarks>The underlying <see cref="IReferenceData.Mappings"/> are included during add; if they are maintained (see <see cref="IReferenceData.SetMapping{T}(string, T)"/>) after these will not be included. Also, where the item
        /// implements <see cref="IReadOnly"/> then <see cref="IReadOnly.MakeReadOnly"/> will be invoked during the add.</remarks>
        public void Add(TRef item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == null)
                throw new ArgumentException("Id must not be null.", nameof(item));

            if (item.Code == null)
                throw new ArgumentException("Code must not be null.", nameof(item));

            lock (_lock)
            {
                if (_rdcId.Values.Contains(item))
                    throw new ArgumentException($"Item already exists within the collection.", nameof(item));

                if (_rdcId.ContainsKey(item.Id))
                    throw new ArgumentException($"Item with Id '{item.Id}' already exists within the collection.", nameof(item));

                if (_rdcCode.ContainsKey(item.Code))
                    throw new ArgumentException($"Item with Code '{item.Code!}' already exists within the collection.", nameof(item));

                if (item.HasMappings)
                {
                    _mappingsDict ??= new();

                    // Make sure there are no duplicates.
                    foreach (var map in item.Mappings)
                    {
                        if (_mappingsDict.ContainsKey((map.Key, map.Value)))
                            throw new ArgumentException($"Item with Mapping Key '{map.Key}' and Value '{map.Value}' already exists within the collection.");
                    }

                    // Now add 'em in.
                    foreach (var map in item.Mappings)
                    {
                        _mappingsDict.Add((map.Key, map.Value), item);
                    }
                }

                // Add to the underlying dictionaries.
                _rdcId.TryAdd(item.Id, item);
                _rdcCode.TryAdd(item.Code, item);

                if (item is IReadOnly ro)
                    ro.MakeReadOnly();
            }
        }

        /// <summary>
        /// Adds the <paramref name="collection"/> to the <see cref="ReferenceDataCollection{TId, TRef}"/>.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        public void AddRange(IEnumerable<TRef> collection)
        {
            if (collection == null)
                return;

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        /// <inheritdoc/>
        public bool ContainsId(TId id) => _rdcId.ContainsKey(id);

        /// <inheritdoc/>
        public bool TryGetById(TId id, out TRef? item)
        {
            if (id != null)
                return _rdcId.TryGetValue(id, out item);

            item = default;
            return false;
        }

        /// <inheritdoc/>
        public TRef? GetById(TId id) => id == null ? default : _rdcId[id];

        /// <inheritdoc/>
        public bool ContainsCode(string code) => _rdcCode.ContainsKey(code);

        /// <inheritdoc/>
        public bool TryGetByCode(string code, out TRef? item)
        {
            if (code != null)
                return _rdcCode.TryGetValue(code, out item);

            item = default;
            return false;
        }

        /// <inheritdoc/>
        public TRef? GetByCode(string code) => code == null ? default : _rdcCode[code];

        /// <inheritdoc/>
        public bool ContainsMappingValue<T>(string name, T value) where T : IComparable<T>, IEquatable<T> => _mappingsDict != null && _mappingsDict.ContainsKey((name, value));

        /// <inheritdoc/>
        public bool TryGetByMappingValue<T>(string name, T value, out TRef? item) where T : IComparable<T>, IEquatable<T>
        {
            if (_mappingsDict != null)
                return _mappingsDict.TryGetValue((name, value), out item);

            item = default;
            return false;
        }

        /// <inheritdoc/>
        public TRef? GetByMappingValue<T>(string name, T value) where T : IComparable<T>, IEquatable<T> => TryGetByMappingValue(name, value, out var item) ? item : default;

        /// <summary>
        /// Gets a list of all items (excluding where <i>not</i> <see cref="IsItemValid(TRef)"/> ) sorted by the <see cref="SortOrder"/> value.
        /// </summary>
        /// <value>An <see cref="IList{T}"/> containing the selected items.</value>
        /// <remarks>This is provided as a property to more easily support binding; it encapsulates the following method invocation: <c><see cref="GetList"/>(SortOrder, null, null);</c></remarks>
        public IList<TRef> AllList => GetList(SortOrder, null, true);

        /// <summary>
        /// Gets a list of all active (<see cref="IsItemActive(TRef)"/> and <see cref="IsItemValid(TRef)"/>) items sorted by the <see cref="SortOrder"/> value.
        /// </summary>
        /// <value>An <see cref="IList{TItem}"/> containing the selected items.</value>
        /// <remarks>This is provided as a property to more easily support binding; it encapsulates the following method invocation: <c><see cref="GetList"/>(SortOrder, true, true);</c></remarks>
        public IList<TRef> ActiveList => GetList(SortOrder, true, true);

        /// <summary>
        /// Gets a list of <typeparamref name="TRef"/> items from the collection using the specified criteria.
        /// </summary>
        /// <param name="sortOrder">Defines the <see cref="ReferenceDataSortOrder"/>; <c>null</c> indicates to use the defined <see cref="SortOrder"/>.</param>
        /// <param name="isActive">Indicates whether the list should include values with the same <see cref="IReferenceData.IsActive"/> value; otherwise, <c>null</c> indicates all.</param>
        /// <param name="isValid">Indicates whether the list should include values with the same <see cref="IsItemValid"/> value; otherwise, <c>null</c> indicates all.</param>
        /// <remakes>This is leveraged by <see cref="AllList"/> and <see cref="ActiveList"/>.</remakes>
        public List<TRef> GetList(ReferenceDataSortOrder? sortOrder = null, bool? isActive = null, bool? isValid = null)
        {
            if (_rdcId.IsEmpty)
                return new List<TRef>();

            var list = from rd in _rdcId.Values select rd;
            if (isActive != null)
                list = list.Where(x => IsItemActive(x));

            if (isValid != null)
                list = list.Where(x => IsItemValid(x));

            list = (sortOrder ?? SortOrder) switch
            {
                ReferenceDataSortOrder.Id => list.OrderBy(x => x.Id),
                ReferenceDataSortOrder.Code => list.OrderBy(x => x.Code),
                ReferenceDataSortOrder.Text => list.OrderBy(x => x.Text).ThenBy(x => x.Code),
                _ => list.OrderBy(x => x.SortOrder).ThenBy(x => x.Text).ThenBy(x => x.Code)
            };

            return list.ToList();
        }

        /// <summary>
        /// Determines whether the <paramref name="item"/> is considered active and therefore accessible from within the collection.
        /// </summary>
        /// <param name="item">The item to validate.</param>
        /// <returns><c>true</c> indicates active; otherwise, <c>false</c>.</returns>
        /// <remarks>By default checks <see cref="IReferenceData.IsActive"/>.</remarks>
        protected virtual bool IsItemActive(TRef item) => item.IsActive;

        /// <summary>
        /// Determines whether the <paramref name="item"/> is considered valid and therefore accessible from within the collection.
        /// </summary>
        /// <param name="item">The item to validate.</param>
        /// <returns><c>true</c> indicates valid; otherwise, <c>false</c>.</returns>
        /// <remarks>By default checks <see cref="IReferenceData.IsValid"/>.</remarks>
        protected virtual bool IsItemValid(TRef item) => item.IsValid;

        #region ICollection

        /// <inheritdoc/>
        public ICollection<TId> Keys => _rdcId.Keys;

        /// <inheritdoc/>
        public ICollection<TRef> Values => _rdcId.Values;

        /// <inheritdoc/>
        public int Count => _rdcId.Count;

        /// <inheritdoc/>
        bool ICollection<TRef>.IsReadOnly => ((ICollection<KeyValuePair<TId, TRef>>)_rdcId).IsReadOnly;

        /// <inheritdoc/>
        public bool Contains(TRef item) => _rdcId.Values.Contains(item);

        /// <inheritdoc/>
        void ICollection<TRef>.CopyTo(TRef[] array, int arrayIndex) => throw new NotSupportedException();

        /// <inheritdoc/>
        bool ICollection<TRef>.Remove(TRef item) => throw new NotSupportedException();

        /// <inheritdoc/>
        /// <remarks>Only items that are <see cref="IsItemValid(TRef)"/> are enumerated. There is no implied sort order; use <see cref="GetList(ReferenceDataSortOrder?, bool?, bool?)"/> for sorted lists.</remarks>
        public IEnumerator<TRef> GetEnumerator()
        {
            foreach (TRef item in _rdcId.Values)
            {
                if (IsItemValid(item))
                    yield return item;
            }
        }

        /// <inheritdoc/>
        /// <remarks>Only items that are <see cref="IsItemValid(TRef)"/> are enumerated. There is no implied sort order; use <see cref="GetList(ReferenceDataSortOrder?, bool?, bool?)"/> for sorted lists.</remarks>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}