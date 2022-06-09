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
    /// Provides the capabilities for a special purpose <typeparamref name="TRef"/> collection specifically for managing a referenced list of <i>serialization identifiers</i> being the underlying <see cref="IReferenceData.Code"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData{TId}"/> <see cref="Type"/>.</typeparam>
    public class ReferenceDataCodeList<TRef> : IReferenceDataCodeList, IList<TRef>, INotifyCollectionChanged where TRef : class, IReferenceData, new()
    {
        private readonly List<string?> _codes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeList{TRef}"/> class.
        /// </summary>
        public ReferenceDataCodeList() => _codes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeList{TRef}"/> class with a reference to an external <see cref="IReferenceData.Code"/> list.
        /// </summary>
        /// <param name="codes">A reference to the external <see cref="IReferenceData.Code"/> list; it is this list that will be maintained by this collection. Changes made to the referenced list will bypass <see cref="INotifyCollectionChanged"/>.</param>
        public ReferenceDataCodeList(ref List<string?>? codes) => _codes = codes ?? new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeList{TRef}"/> class with a list of items.
        /// </summary>
        /// <param name="items">The list of <see cref="IReferenceData"/> items.</param>
        public ReferenceDataCodeList(IEnumerable<TRef> items) => _codes = new((items ?? Array.Empty<TRef>()).Select(x => x.Code));

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeList{TRef}"/> class with a <see cref="IReferenceData.Code"/> array.
        /// </summary>
        /// <param name="codes">The <see cref="IReferenceData.Code"/> array.</param>
        public ReferenceDataCodeList(params string?[] codes) => _codes = new(codes);

        /// <summary>
        /// Creates a new <see cref="IReferenceData.Code"/> list from the underlying contents.
        /// </summary>
        /// <returns>A new <see cref="IReferenceData.Code"/> list list.</returns>
        public List<string?> ToCodeList() => new(_codes);

        /// <inheritdoc/>
        List<IReferenceData> IReferenceDataCodeList.ToRefDataList() => new(this);

        /// <summary>
        /// Creates a new <typeparamref name="TRef"/> list from the underlying contents.
        /// </summary>
        /// <returns>A new <typeparamref name="TRef"/> list</returns>
        public List<TRef> ToRefDataList() => this.ToList();

        /// <summary>
        /// Creates a new <see cref="IIdentifier{TId}.Id"/> list from the underlying contents.
        /// </summary>
        /// <typeparam name="TId">The <see cref="IIdentifier.Id"/> <see cref="Type"/>.</typeparam>
        /// <returns>A new <see cref="IIdentifier{TId}.Id"/> list</returns>
        public List<TId?> ToIdList<TId>() => this.Select(x => (TId?)x.Id).ToList();

        /// <summary>
        /// Indicates whether the collection contains invalid items (i.e. not <see cref="IReferenceData.IsValid"/>).
        /// </summary>
        /// <returns><c>true</c> indicates that invalid items exist; otherwise, <c>false</c>.</returns>
        public bool HasInvalidItems => this.Any(x => x == null || !x.IsValid);

        /// <summary>
        /// Gets the item for the specified <paramref name="code"/>.
        /// </summary>
        private static TRef GetItem(string? code)
        {
            if (code != null && ExecutionContext.HasCurrent)
            {
                var rdc = ReferenceDataOrchestrator.Current[typeof(TRef)];
                if (rdc != null && rdc.TryGetByCode(code, out var rd))
                    return (TRef)rd!;
            }

            var rdx = new TRef { Code = code };
            rdx.SetInvalid();
            return rdx;
        }


        #region IList

        /// <inheritdoc/>
        public TRef this[int index] 
        { 
            get => GetItem(_codes[index]); 

            set
            {
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, GetItem(_codes[index]!));
                _codes[index] = value?.Code;
                OnCollectionChanged(e);
            }
        }

        /// <inheritdoc/>
        public int Count => _codes.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => ((IList)_codes).IsReadOnly;

        /// <inheritdoc/>
        public void Add(TRef item)
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _codes.Count);
            _codes.Add(item?.Code);
            OnCollectionChanged(e);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this));
            _codes.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(TRef item) => _codes.Contains(item?.Code);

        /// <inheritdoc/>
        public void CopyTo(TRef[] array, int arrayIndex)
        {
            if (array == null || array.Length == 0)
                return;

            var codes = new string?[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                codes[i] = array[i]?.Code;
            }

            _codes.CopyTo(codes, arrayIndex);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, array, arrayIndex));
        }

        /// <inheritdoc/>
        public IEnumerator<TRef> GetEnumerator()
        {
            foreach (string? code in _codes)
            {
                yield return GetItem(code!);
            }
        }

        /// <inheritdoc/>
        public int IndexOf(TRef item) => _codes.IndexOf(item?.Code);

        /// <inheritdoc/>
        public void Insert(int index, TRef item)
        {
            _codes.Insert(index, item?.Code);
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
            _codes.RemoveAt(index);
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