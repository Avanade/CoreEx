// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents the base capabilities for a full featured <b>Entity</b>.
    /// </summary>
    /// <typeparam name="TSelf">The entity <see cref="Type"/> itself.</typeparam>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBase<TSelf> : EntityBase, IEditableObject, ICloneable<TSelf>, ICopyFrom<TSelf>, IEquatable<TSelf>, ICleanUp, IChangeTrackingLogging where TSelf : EntityBase<TSelf>
    {
        private TSelf? _editCopy;

#pragma warning disable IDE0060 // Remove unused parameter; needed to support inheritance.
        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="from">The object to copy from.</param>
        protected void CopyFrom(object? from) { }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="from">The object to copy from.</param>
        public abstract void CopyFrom(TSelf from);

        /// <summary>
        /// Creates a deep copy of the <see cref="EntityBase{TSelf}"/>.
        /// </summary>
        /// <returns>A deep copy of the <see cref="EntityBase{TSelf}"/>.</returns>
        public abstract TSelf Clone();

        /// <inheritdoc/>
        public override bool Equals(object? obj) => (obj is TSelf other) && Equals(other);

        /// <inheritdoc/>
        public abstract bool Equals(TSelf other);

        /// <summary>
        /// Facilitates the equals comparison to determine whether the specified <paramref name="other"/> is equal to the current instance by comparing the values of all the properties.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <param name="compare">The function to perform extended property comparisons.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        /// <remarks>Performs standardized <c>null</c> and reference equality comparisons before invoking the <paramref name="compare"/> function.</remarks>
        protected bool Equals(TSelf other, Func<bool> compare)
        {
            if (((object)other!) == ((object)this))
                return true;
            else if (((object)other!) == null)
                return false;

            return compare?.Invoke() ?? true;
        }

        /// <summary>
        /// Returns a hash code for the <see cref="EntityBase{TSelf}"/> (always returns the same value regardless; inheritors should override).
        /// </summary>
        /// <returns>A hash code for the <see cref="EntityBase{TSelf}"/>.</returns>
        public override int GetHashCode() => 0;

        /// <summary>
        /// Resets the entity state to unchanged by accepting the changes (resets <see cref="ChangeTracking"/>).
        /// </summary>
        /// <remarks>Ends and commits the entity changes (see <see cref="EndEdit"/>).</remarks>
        public override void AcceptChanges()
        {
            base.AcceptChanges();
            _editCopy = null;
            ChangeTracking = null;
        }

        #region IEditableObject

        /// <summary>
        /// Begins an edit on an entity.
        /// </summary>
        /// <remarks>Sets the entity state to unchanged (see <see cref="AcceptChanges"/>).</remarks>
        public void BeginEdit()
        {
            // Exit where already in edit mode.
            if (_editCopy != null)
                return;

            AcceptChanges();
            _editCopy = Clone();
        }

        /// <summary>
        /// Discards the entity changes since the last <see cref="BeginEdit"/>.
        /// </summary>
        /// <remarks>Resets the entity state to unchanged (see <see cref="AcceptChanges"/>) after the changes have been discarded.</remarks>
        public void CancelEdit()
        {
            if (_editCopy != null)
                CopyFrom(_editCopy);

            AcceptChanges();
        }

        /// <summary>
        /// Ends and commits the entity changes since the last <see cref="BeginEdit"/>.
        /// </summary>
        /// <remarks>Resets the entity state to unchanged (see <see cref="AcceptChanges"/>).</remarks>
        public void EndEdit()
        {
            if (_editCopy != null)
                AcceptChanges();
        }

        #endregion

        #region ICleanup

        /// <summary>
        /// Performs a clean-up of the <see cref="EntityBase{TSelf}"/> resetting property values as appropriate to ensure a basic level of data consistency.
        /// </summary>
        public virtual void CleanUp() { }

        /// <summary>
        /// Indicates whether considered initial; i.e. all properties have their initial value.
        /// </summary>
        public abstract bool IsInitial { get; }

        #endregion

        #region IChangeTracking

        /// <summary>
        /// Determines that until <see cref="AcceptChanges"/> is invoked property changes are to be logged (see <see cref="ChangeTracking"/>).
        /// </summary>
        public virtual void TrackChanges()
        {
            if (ChangeTracking == null)
                ChangeTracking = new StringCollection();
        }

        /// <summary>
        /// Listens to the <see cref="OnPropertyChanged"/> to perform <see cref="ChangeTracking"/>.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (ChangeTracking != null && !ChangeTracking.Contains(propertyName))
                ChangeTracking.Add(propertyName);
        }

        /// <summary>
        /// Lists the properties (names of) that have been changed (note that this property is not JSON serialized).
        /// </summary>
        public StringCollection? ChangeTracking { get; private set; }

        /// <summary>
        /// Indicates whether entity is currently <see cref="ChangeTracking"/>; <see cref="TrackChanges"/> and <see cref="IChangeTracking.AcceptChanges"/>.
        /// </summary>
        public bool IsChangeTracking => ChangeTracking != null;

        #endregion
    }
}