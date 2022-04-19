// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBase"/> collection class.
    /// </summary>
    /// <typeparam name="TEntity">The <see cref="EntityBase"/> <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="EntityBase"/> collection <see cref="Type"/> itself.</typeparam>
    //[System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBaseCollection<TEntity, TSelf> : ObservableCollection<TEntity>, IEntityBaseCollection, IEquatable<TSelf>, ICloneable
        where TEntity : EntityBase 
        where TSelf : EntityBaseCollection<TEntity, TSelf>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBaseCollection{TEntity, TSelf}" /> class.
        /// </summary>
        protected EntityBaseCollection() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBaseCollection{TEntity, TSelf}" /> class.
        /// </summary>
        /// <param name="collection">The entities to add.</param>
        protected EntityBaseCollection(IEnumerable<TEntity> collection) : base(collection) { }

        /// <summary>
        /// Adds the items of the specified collection to the end of the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        void IEntityBaseCollection.AddRange(IEnumerable collection)
        {
            if (collection == null)
                return;

            foreach (TEntity item in collection)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Adds the items of the specified collection to the end of the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        public void AddRange(IEnumerable<TEntity> collection)
        {
            if (collection == null)
                return;

            foreach (TEntity item in collection)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Gets a <see cref="IEnumerable{TEntity}"/> wrapper around the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.
        /// </summary>
        /// <remarks>This is provided to enable the likes of <b>LINQ</b> based queries over the collection.</remarks>
        public new IEnumerable<TEntity> Items => base.Items;

        /// <summary>
        /// Creates a deep copy of the entity collection (all items will also be cloned).
        /// </summary>
        /// <returns>A deep copy of the entity collection.</returns>
        public object Clone()
        {
            var clone = new TSelf();
            foreach (var item in this)
            {
                clone.Add((TEntity)(item as ICloneable).Clone());
            }

            return clone;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => (obj is TSelf other) && Equals(other);

        /// <inheritdoc/>
        public bool Equals(TSelf? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            else if (other == null)
                return false;
            else if (Count != other.Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (!this[i].Equals(other[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(EntityBaseCollection<TEntity, TSelf>? a, EntityBaseCollection<TEntity, TSelf>? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(EntityBaseCollection<TEntity, TSelf>? a, EntityBaseCollection<TEntity, TSelf>? b) => !Equals(a, b);

        /// <summary>
        /// Returns a hash code for the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.
        /// </summary>
        /// <returns>A hash code for the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var item in this)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Performs a clean-up of the <see cref="EntityBaseCollection{TEntity, TSelf}"/> resetting item values as appropriate to ensure a basic level of data consistency.
        /// </summary>
        public void CleanUp() => ApplyAction(EntityAction.CleanUp);

        /// <summary>
        /// Collections do not support an initial state; will always be <c>false</c>.
        /// </summary>
        /// <remarks>The collection reference should be set to <c>null</c> to achieve <see cref="IInitial.IsInitial"/>.</remarks>
        bool IInitial.IsInitial => false;

        /// <summary>
        /// Overrides the <see cref="ObservableCollection{TEntity}.OnCollectionChanged"/> method.
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
                    var ei = (EntityBase)item;
                    if (ei != null)
                        ei.PropertyChanged -= Item_PropertyChanged;
                }
            }

            if (e?.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    var ei = (EntityBase)item;
                    if (ei != null)
                        ei.PropertyChanged += Item_PropertyChanged;
                }
            }

            IsChanged = true;
        }

        /// <summary>
        /// Updates IsChanged where required.
        /// </summary>
        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
            OnPropertyChanged(new ItemPropertyChangedEventArgs(sender, e.PropertyName));
        }

        /// <summary>
        /// Resets the entity state to unchanged by accepting the changes.
        /// </summary>
        /// <remarks>This will trigger an <see cref="EntityCore.AcceptChanges"/> for each item.</remarks>
        public virtual void AcceptChanges()
        {
            ApplyAction(EntityAction.AcceptChanges);
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
            ApplyAction(EntityAction.MakeReadOnly);
            IsChanged = false;
            IsReadOnly = true;
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            CheckReadOnly(() =>
            {
                CheckReentrancy();
                var items = new List<EntityBase>(this);
                base.ClearItems();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items));
            });
        }

        /// <inheritdoc/>
        protected override void InsertItem(int index, TEntity item) => CheckReadOnly(() => base.InsertItem(index, item));

        /// <inheritdoc/>
        protected override void MoveItem(int oldIndex, int newIndex) => CheckReadOnly(() => base.MoveItem(oldIndex, newIndex));

        /// <inheritdoc/>
        protected override void RemoveItem(int index) => CheckReadOnly(() => base.RemoveItem(index));

        /// <inheritdoc/>
        protected override void SetItem(int index, TEntity item) => CheckReadOnly(() => base.SetItem(index, item));

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
        /// Performs the specified action.
        /// </summary>
        private void ApplyAction(EntityAction action)
        {
            foreach (var item in this)
            {
                switch (action)
                {
                    case EntityAction.CleanUp:
                        item.CleanUp();
                        break;

                    case EntityAction.AcceptChanges:
                        item.AcceptChanges();
                        break;

                    case EntityAction.MakeReadOnly:
                        item.MakeReadOnly();
                        break;
                }
            }
        }
    }
}