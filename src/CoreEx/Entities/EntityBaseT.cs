// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents the base capabilities for a full featured <b>Entity</b>.
    /// </summary>
    /// <typeparam name="TSelf">The entity <see cref="Type"/> itself.</typeparam>
    /// <remarks>To leverage this base class correctly the following <b>must</b> be overridden: <see cref="Equals(TSelf)"/>, <see cref="GetHashCode"/>, <see cref="CopyFrom(TSelf)"/>, <see cref="IsInitial"/> and <see cref="EntityCore.OnApplyAction(EntityAction)"/>.
    /// <para>Also, note that when inheriting from a class that already inherits from <see cref="EntityBase{TSelf}"/> then the following interfaces must be added for the new type: <see cref="ICloneable{T}"/>, <see cref="ICopyFrom{T}"/>
    /// and <see cref="IEquatable{T}"/> to ensure correctness and consistency.</para></remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBase<TSelf> : EntityBase, ICloneable<TSelf>, ICopyFrom<TSelf>, IEquatable<TSelf>, ICleanUp, IInitial where TSelf : EntityBase<TSelf>, new()
    {
#pragma warning disable IDE0060 // Remove unused parameter; needed to support inheritance.
        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="from">The object to copy from.</param>
        public void CopyFrom(EntityBase<TSelf> from) { }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="from">The object to copy from.</param>
        public virtual void CopyFrom(TSelf from) { }

        /// <summary>
        /// Creates a deep clone (copy) of the <see cref="EntityBase{TSelf}"/>.
        /// </summary>
        /// <returns>A deep clone of the <see cref="EntityBase{TSelf}"/>.</returns>
        public TSelf Clone()
        {
            var clone = new TSelf();
            clone.CopyFrom((TSelf)this);
            return clone;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => (obj is TSelf other) && Equals(other);

        /// <inheritdoc/>
        public virtual bool Equals(TSelf other) => true;

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(EntityBase<TSelf>? a, EntityBase<TSelf>? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(EntityBase<TSelf>? a, EntityBase<TSelf>? b) => !Equals(a, b);

        /// <summary>
        /// Returns a hash code for the <see cref="EntityBase{TSelf}"/> (always returns the same value regardless; inheritors should override).
        /// </summary>
        /// <returns>A hash code for the <see cref="EntityBase{TSelf}"/>.</returns>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (this is IIdentifier ii)
                return $"{base.ToString()} Id={ii.GetIdentifier()}";
            else if (this is IPrimaryKey pk)
                return $"{base.ToString()} PrimaryKey={pk}";
            else
                return base.ToString();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// This will trigger the <see cref="EntityCore.OnApplyAction(EntityAction)"/> with <see cref="EntityAction.CleanUp"/>.
        public void CleanUp() => OnApplyAction(EntityAction.CleanUp);

        /// <inheritdoc/>
        public virtual bool IsInitial => true;
    }
}