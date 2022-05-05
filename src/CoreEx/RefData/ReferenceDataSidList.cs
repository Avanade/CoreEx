// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the capabilities for a special purpose <typeparamref name="TRef"/> collection specifically for managing a referenced list of <b>Serialization Identifiers</b> (SIDs) being the underlying <see cref="IReferenceData.Code"/>.
    /// </summary>
    /// <typeparam name="TId">The <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    public class ReferenceDataSidList<TId, TRef> : IReferenceDataSidList, IList<TRef>, INotifyCollectionChanged where TId : IComparable<TId>, IEquatable<TId> where TRef : class, IReferenceData<TId>, new()
    {
        private readonly List<string?> _sids;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataSidList{TId, TRef}"/> class.
        /// </summary>
        public ReferenceDataSidList() => _sids = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataSidList{TId, TRef}"/> class with a reference to an external <b>Serialization Identifier</b> (SID) list.
        /// </summary>
        /// <param name="sids">A reference to the externa; <b>Serialization Identifier</b> (SID) list; it is this list that will be maintained by this collection. Changes made to the referenced list will bypass <see cref="INotifyCollectionChanged"/>.</param>
        public ReferenceDataSidList(ref List<string?>? sids) => _sids = sids ?? new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataSidList{TId, TRef}"/> class with a list of items.
        /// </summary>
        /// <param name="items">The list of <see cref="IReferenceData"/> items.</param>
        public ReferenceDataSidList(IEnumerable<TRef> items) => _sids = new((items ?? Array.Empty<TRef>()).Select(x => x.Code));

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataSidList{TId, TRef}"/> class with a list of <b>Serialization Identifiers</b> (SIDs).
        /// </summary>
        /// <param name="sids">The list of <b>Serialization Identifiers</b> (SIDs).</param>
        public ReferenceDataSidList(params string?[] sids) => _sids = new(sids);

        /// <summary>
        /// Creates a new <see cref="IReferenceData.Code"/> list from the underlying contents.
        /// </summary>
        /// <returns>A new <see cref="IReferenceData.Code"/> list list.</returns>
        public List<string?> ToCodeList() => new(_sids);

        /// <inheritdoc/>
        List<IReferenceData> IReferenceDataSidList.ToRefDataList() => new(this);

        /// <summary>
        /// Creates a new <typeparamref name="TRef"/> list from the underlying contents.
        /// </summary>
        /// <returns>A new <typeparamref name="TRef"/> list</returns>
        public List<TRef> ToRefDataList() => this.ToList();

        /// <summary>
        /// Creates a new <see cref="IIdentifier{TId}.Id"/> list from the underlying contents.
        /// </summary>
        /// <returns>A new <see cref="IIdentifier{TId}.Id"/> list</returns>
        public List<TId?> ToIdList() => this.Select(x => x.Id).ToList();

        /// <summary>
        /// Indicates whether the collection contains invalid items (i.e. not <see cref="IReferenceData.IsValid"/>).
        /// </summary>
        /// <returns><c>true</c> indicates that invalid items exist; otherwise, <c>false</c>.</returns>
        public bool HasInvalidItems => this.Any(x => x == null || !x.IsValid);

        /// <summary>
        /// Gets the item for the sid/code.
        /// </summary>
        private static TRef GetItem(string? sid)
        {
            if (sid != null && ExecutionContext.HasCurrent)
            {
                var rdc = ReferenceDataOrchestrator.Current[typeof(TRef)];
                if (rdc != null && rdc.TryGetByCode(sid, out var rd))
                    return (TRef)rd!;
            }

            var rdx = new TRef { Code = sid };
            rdx.SetInvalid();
            return rdx;
        }


        #region IList

        /// <inheritdoc/>
        public TRef this[int index] 
        { 
            get => GetItem(_sids[index]); 

            set
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, GetItem(_sids[index]!));
                _sids[index] = value?.Code;
                OnCollectionChanged(e);
            }
        }

        /// <inheritdoc/>
        public int Count => _sids.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => ((IList)_sids).IsReadOnly;

        /// <inheritdoc/>
        public void Add(TRef item)
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _sids.Count);
            _sids.Add(item?.Code);
            OnCollectionChanged(e);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this));
            _sids.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(TRef item) => _sids.Contains(item?.Code);

        /// <inheritdoc/>
        public void CopyTo(TRef[] array, int arrayIndex)
        {
            if (array == null || array.Length == 0)
                return;

            var sids = new string?[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                sids[i] = array[i]?.Code;
            }

            _sids.CopyTo(sids, arrayIndex);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, array, arrayIndex));
        }

        /// <inheritdoc/>
        public IEnumerator<TRef> GetEnumerator()
        {
            foreach (string? sid in _sids)
            {
                yield return GetItem(sid!);
            }
        }

        /// <inheritdoc/>
        public int IndexOf(TRef item) => _sids.IndexOf(item?.Code);

        /// <inheritdoc/>
        public void Insert(int index, TRef item)
        {
            _sids.Insert(index, item?.Code);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <inheritdoc/>
        public bool Remove(TRef item)
        {
            var index = IndexOf(item);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this[index], index);
            _sids.RemoveAt(index);
            OnCollectionChanged(e);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        #endregion
    }
}