// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the base capabilities for the <see cref="EntityBase{TSelf}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBase : EntityCore, ICleanUp, IInitial, ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBase"/> class.
        /// </summary>
        internal EntityBase() { }

        /// <summary>
        /// Copies (<see cref="ICopyFrom{T}.CopyFrom(T)"/>) or clones (<see cref="ICloneable.Clone"/>) the <paramref name="from"/> value.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <param name="from">The from value.</param>
        /// <param name="to">The to value (required to support a <see cref="ICopyFrom{T}.CopyFrom(T)"/>).</param>
        /// <returns>The resulting to value.</returns>
        /// <remarks>A <see cref="ICopyFrom{T}.CopyFrom(T)"/> will be attempted first where supported, then a <see cref="ICloneable.Clone"/>; otherwise, a <see cref="InvalidOperationException"/> will be thrown.
        /// <i>Note:</i> <see cref="ICopyFrom{T}"/> is not supported for collections.</remarks>
        /// <exception cref="InvalidOperationException">Thrown where neither <see cref="ICopyFrom{T}"/>) or <see cref="ICloneable"/> are supported.</exception>
        protected static T? CopyOrClone<T>(T? from, T? to) where T : class
        {
            if (from == default)
                return default!;

            if (to == default && from is ICloneable c)
                return (T)c.Clone();
            else if (to is ICopyFrom<T> cf)
            {
                cf.CopyFrom(from);
                return to;
            }
            else if (from is ICloneable c2)
                return (T)c2.Clone();

            throw new ArgumentException("The Type of the value must support ICopyFrom<T> and/or ICloneable<T> as a minimum to enable a CopyOrClone.", nameof(from));
        }

        /// <summary>
        /// Creates a clone of <see cref="Type"/> <typeparamref name="T"/> by instantiating a new instance and performing a <see cref="ICopyFrom{T}.CopyFrom(T)"/> from the <paramref name="from"/> value.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="from">The value to copy from.</param>
        /// <returns>A new cloned instance.</returns>
        protected static T CreateClone<T>(T from) where T : ICopyFrom<T>, new()
        {
            var clone = new T();
            clone.CopyFrom(from);
            return clone;
        }

        /// <summary>
        /// Creates a deep copy of the entity.
        /// </summary>
        /// <returns>A deep copy of the entity.</returns>
        public abstract object Clone();

        /// <inheritdoc/>
        public abstract void CleanUp();

        /// <inheritdoc/>
        public virtual bool IsInitial => true;
    }
}