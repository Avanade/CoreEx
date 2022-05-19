// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBase"/> dictionary class with a key of Type <see cref="string"/>.
    /// </summary>
    /// <typeparam name="TEntity">The <see cref="EntityBase"/> <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="EntityBase"/> dictionary <see cref="Type"/> itself.</typeparam>
    /// <remarks>The key is constrained to be of Type <see cref="string"/> as this is a constraint related to JSON serialization.</remarks>
    public class EntityBaseDictionary<TEntity, TSelf> : ObservableDictionary<string, TEntity>, INotifyPropertyChanged, INotifyCollectionItemChanged, IEntityBaseCollection
        where TEntity : EntityBase, new()
        where TSelf : EntityBaseDictionary<TEntity, TSelf>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBaseDictionary{TEntity, TSelf}" /> class using the <see cref="StringComparer.OrdinalIgnoreCase"/> for the comparer.
        /// </summary>
        protected EntityBaseDictionary() : base(StringComparer.OrdinalIgnoreCase) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBaseDictionary{TEntity, TSelf}" /> class using the <see cref="StringComparer.OrdinalIgnoreCase"/> for the comparer adding the passed <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The items to add.</param>
        protected EntityBaseDictionary(IEnumerable<KeyValuePair<string, TEntity>> collection) : base(collection, StringComparer.OrdinalIgnoreCase) { }

        /// <summary>
        /// Creates a deep copy of the entity dictionary (all items will also be cloned).
        /// </summary>
        /// <returns>A deep copy of the entity dictionary.</returns>
        public object Clone()
        {
            var clone = new TSelf();
            this.ForEach(item => clone.Add(item.Key, (TEntity)item.Value.Clone()));
            return clone;
        }

        /// <inheritdoc/>
        public override bool Equals(object? other)
        {
            if (other is not TSelf otherv)
                return false;
            else if (ReferenceEquals(this, other))
                return true;
            else if (other == null)
                return false;
            else if (Count != otherv.Count)
                return false;

            foreach (var item in this)
            {
                if (!otherv.TryGetValue(item.Key, out var value))
                    return false;

                if (!item.Value.Equals(value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="EntityBaseDictionary{TEntity, TSelf}"/> A.</param>
        /// <param name="b"><see cref="EntityBaseDictionary{TEntity, TSelf}"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(EntityBaseDictionary<TEntity, TSelf>? a, EntityBaseDictionary<TEntity, TSelf>? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(EntityBaseDictionary<TEntity, TSelf>? a, EntityBaseDictionary<TEntity, TSelf>? b) => !Equals(a, b);

        /// <summary>
        /// Returns a hash code for the <see cref="EntityBaseDictionary{TEntity, TSelf}"/>.
        /// </summary>
        /// <returns>A hash code for the <see cref="EntityBaseDictionary{TEntity, TSelf}"/>.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var item in this)
            {
                hash.Add(item.Key.GetHashCode());
                hash.Add(item.Value.GetHashCode());
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Performs a clean-up of the <see cref="EntityBaseCollection{TEntity, TSelf}"/> resetting item values as appropriate to ensure a basic level of data consistency.
        /// </summary>
        public void CleanUp() => this.ForEach(item => item.Value.CleanUp());

        /// <summary>
        /// Collections do not support an initial state; will always be <c>false</c>.
        /// </summary>
        /// <remarks>The collection reference should be set to <c>null</c> to achieve <see cref="IInitial.IsInitial"/>.</remarks>
        bool IInitial.IsInitial => false;

        /// <summary>
        /// Overrides the <see cref="ObservableDictionary{TKey, TValue}.OnCollectionChanged"/> method.
        /// </summary>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.</param>
        /// <remarks>Sets <see cref="IsChanged"/> to <c>true</c>.</remarks>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Collection is read only; item(s) cannot be added, updated or deleted.");

            base.OnCollectionChanged(e);

            if (e?.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    var ci = (KeyValuePair<string, TEntity>)item;
                    var ei = (EntityBase)ci.Value;
                    if (ei != null)
                        ei.PropertyChanged -= Item_PropertyChanged;
                }
            }

            if (e?.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    var ci = (KeyValuePair<string, TEntity>)item;
                    var ei = (EntityBase)ci.Value;
                    if (ei != null)
                        ei.PropertyChanged += Item_PropertyChanged;
                }
            }

            IsChanged = true;
        }

        /// <summary>
        /// Handles the item change and propogates.
        /// </summary>
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
            OnItemChanged(new CollectionItemChangedEventArgs(sender, e.PropertyName));
        }

        /// <summary>
        /// Raises the <see cref="CollectionItemChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="CollectionItemChangedEventArgs"/>.</param>
        protected void OnItemChanged(CollectionItemChangedEventArgs e) => CollectionItemChanged?.Invoke(this, e);

        /// <inheritdoc/>
        public event CollectionItemChangedEventHandler? CollectionItemChanged;

        /// <summary>
        /// Resets the entity state to unchanged by accepting the changes.
        /// </summary>
        /// <remarks>This will trigger an <see cref="EntityCore.AcceptChanges"/> for each item.</remarks>
        public virtual void AcceptChanges()
        {
            this.ForEach(item => item.Value.AcceptChanges());
            IsChanged = false;
        }

        /// <summary>
        /// Indicates whether the collection has changed.
        /// </summary>
        public bool IsChanged { get; private set; }

        /// <inheritdoc/>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>This will trigger a <see cref="EntityCore.MakeReadOnly"/> for each item.</remarks>
        public void MakeReadOnly()
        {
            this.ForEach(item => item.Value.MakeReadOnly());
            IsChanged = false;
            IsReadOnly = true;
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            CheckReadOnly(() =>
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this));
                base.ClearItems();
            });
        }

        /// <inheritdoc/>
        protected override void AddItem(KeyValuePair<string, TEntity> item) => CheckReadOnly(() => base.AddItem(item));

        /// <inheritdoc/>
        protected override void ReplaceItem(KeyValuePair<string, TEntity> oldItem, KeyValuePair<string, TEntity> newItem) => CheckReadOnly(() => base.ReplaceItem(oldItem, newItem));

        /// <inheritdoc/>
        protected override bool RemoveItem(KeyValuePair<string, TEntity> item) => CheckReadOnly(() => base.RemoveItem(item));

        /// <summary>
        /// Checks if readonly and throws; otherwise, executes action.
        /// </summary>
        private void CheckReadOnly(Action action)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Collection is read only; item(s) cannot be added, updated or deleted.");
            else
                action();
        }

        /// <summary>
        /// Checks if readonly and throws; otherwise, executes action.
        /// </summary>
        private bool CheckReadOnly(Func<bool> func)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Collection is read only; item(s) cannot be added, updated or deleted.");
            else
                return func();
        }
    }
}