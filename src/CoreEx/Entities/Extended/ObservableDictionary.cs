// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// An observable <see cref="Dictionary{TKey, TValue}"/> that supports <see cref="INotifyCollectionChanged"/>.
    /// </summary>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dict;
        private Func<TKey, TKey>? _keyModifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> with the default <see cref="IEqualityComparer{T}"/> for the type of the key.
        /// </summary>
        public ObservableDictionary() => _dict = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> with the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or null to use the default <see cref="IEqualityComparer{T}"/> for the type of the key.</param>
        public ObservableDictionary(IEqualityComparer<TKey> comparer) => _dict = new(comparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> with the default <see cref="IEqualityComparer{T}"/> for the type of the key adding the passed <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The items to add.</param>
        public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) => _dict = new(collection);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> with the specified <paramref name="comparer"/> adding the passed <paramref name="collection"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or null to use the default <see cref="IEqualityComparer{T}"/> for the type of the key.</param>
        /// <param name="collection">The items to add.</param>
        public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) => _dict = new(collection, comparer);

        /// <summary>
        /// Gets or sets the function that enabled modification of the key before usage.
        /// </summary>
        /// <remarks>Enables an opportunity to modify the key before used internally; for example, to modify so that the key value is always uppercase.
        /// <para>The <see cref="OnModifyKey"/> by default leverages this function; however, the <see cref="OnModifyKey"/> when overridden may chose to ignore.</para></remarks>
        public Func<TKey, TKey>? KeyModifier 
        {
            get => _keyModifier;
            set
            {
                if (_dict.Count > 0)
                    throw new InvalidOperationException($"{nameof(KeyModifier)} can only be updated when there are no items contained already within the dictionary; i.e. {nameof(Count)} must be zero.");

                _keyModifier = value;
            }
        }

        /// <inheritdoc/>
        public int Count => _dict.Count;

        /// <inheritdoc/>
        bool IDictionary.IsFixedSize => ((IDictionary)_dict).IsFixedSize;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => ((IDictionary)_dict).IsSynchronized;

        /// <inheritdoc/>
        bool IDictionary.IsReadOnly => ((IDictionary)_dict).IsReadOnly;

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).IsReadOnly;

        /// <inheritdoc/>
        object ICollection.SyncRoot => ((IDictionary)_dict).SyncRoot;

        /// <inheritdoc/>
        ICollection IDictionary.Keys => ((IDictionary)_dict).Keys;

        /// <inheritdoc/>
        public IEnumerable<TKey> Keys => ((IReadOnlyDictionary<TKey, TValue>)_dict).Keys;

        /// <inheritdoc/>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => ((IDictionary<TKey, TValue>)_dict).Keys;

        /// <inheritdoc/>
        ICollection IDictionary.Values => ((IDictionary)_dict).Values;

        /// <inheritdoc/>
        public IEnumerable<TValue> Values => ((IReadOnlyDictionary<TKey, TValue>)_dict).Values;

        /// <inheritdoc/>
        ICollection<TValue> IDictionary<TKey, TValue>.Values => ((IDictionary<TKey, TValue>)_dict).Values;

        /// <inheritdoc/>
        public object? this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value!; }

        /// <inheritdoc/>
        TValue IDictionary<TKey, TValue>.this[TKey key] { get => this[key]; set => this[key] = value; }

        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get => _dict[OnModifyKey(key)];

            set
            {
                if (TryGetValue(key, out var old))
                {
                    if (ReferenceEquals(old, value))
                        return;
                    else
                        ReplaceItem(new KeyValuePair<TKey, TValue>(key, old), new KeyValuePair<TKey, TValue>(key, value));
                }
                else
                    AddItem(new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        /// <inheritdoc/>
        void IDictionary.Add(object key, object value) => Add((TKey)key, (TValue)value);

        /// <inheritdoc/>
        public void Add(TKey key, TValue value) => AddItem(new KeyValuePair<TKey, TValue>(key, value));

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => AddItem(item);

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) => ((ICollection)_dict).CopyTo(array, index);

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        void IDictionary.Remove(object key) => Remove((TKey)key);

        /// <inheritdoc/>
        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_dict).GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dict).GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

        /// <inheritdoc/>
        public bool Contains(object key) => ContainsKey((TKey)key);

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Contains(item);

        /// <inheritdoc/>
        public bool ContainsKey(TKey key) => _dict.ContainsKey(OnModifyKey(key));

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue value) => _dict.TryGetValue(OnModifyKey(key), out value);

        /// <inheritdoc/>
        public bool Remove(TKey key) => TryGetValue(key, out var value) && RemoveItem(new KeyValuePair<TKey, TValue>(key, value));

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => RemoveItem(item);

        /// <inheritdoc/>
        public void Clear()
        {
            if (Count > 0)
                ClearItems();
        }

        /// <summary>
        /// Clears all items from the dictionary.
        /// </summary>
        protected virtual void ClearItems()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this.ToList()));
            _dict.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            RaisePropertyChanged();
        }

        /// <summary>
        /// Replaces the item within the dictionary.
        /// </summary>
        /// <param name="oldItem">The old item being replaced.</param>
        /// <param name="newItem">The new replacement item.</param>
        protected virtual void ReplaceItem(KeyValuePair<TKey, TValue> oldItem, KeyValuePair<TKey, TValue> newItem)
        {
            oldItem = new KeyValuePair<TKey, TValue>(OnModifyKey(oldItem.Key), oldItem.Value);
            newItem = new KeyValuePair<TKey, TValue>(OnModifyKey(newItem.Key), newItem.Value);

            _dict[newItem.Key] = newItem.Value;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem));
            RaisePropertyChanged();
        }

        /// <summary>
        /// Adds the item to the dictionary.
        /// </summary>
        /// <param name="item">The <see cref="KeyValuePair{TKey, TValue}"/> that was added.</param>
        protected virtual void AddItem(KeyValuePair<TKey, TValue> item)
        {
            item = new KeyValuePair<TKey, TValue>(OnModifyKey(item.Key), item.Value);

            _dict.Add(item.Key, item.Value);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            RaisePropertyChanged();
        }

        /// <summary>
        /// Removes the item from the dictionary.
        /// </summary>
        /// <param name="item">The <see cref="KeyValuePair{TKey, TValue}"/> that was removed.</param>
        /// <returns><c>true</c> if the item was successfully removed from the <see cref="ObservableDictionary{TKey, TValue}"/>; otherwise, <c>false</c>.</returns>
        protected virtual bool RemoveItem(KeyValuePair<TKey, TValue> item)
        {
            item = new KeyValuePair<TKey, TValue>(OnModifyKey(item.Key), item.Value);
            if (!_dict.Remove(item.Key))
                return false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            RaisePropertyChanged();
            return true;
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/>.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Invokes the <see cref="OnPropertyChanged"/> method.
        /// </summary>
        private void RaisePropertyChanged() => OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/>.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        /// <summary>
        /// Occurs when a property changes, either within the dictionary or to an item within.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Modifies the key before usage; by default uses the <see cref="KeyModifier"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The modified key.</returns>
        /// <remarks>Enables an opportunity to modify the key before used internally; for example, to modify so that it is always uppercase.</remarks>
        protected virtual TKey OnModifyKey(TKey key) => KeyModifier is null ? key : KeyModifier(key);
    }
}