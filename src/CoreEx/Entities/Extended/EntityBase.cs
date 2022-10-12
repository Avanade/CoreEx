// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides the base capabilities for the <see cref="EntityBase{TSelf}"/>.
    /// </summary>
    /// <remarks>Inherits from <see cref="EntityCore"/> and implements <see cref="ICleanUp"/>, <see cref="IInitial"/>, <see cref="ICopyFrom"/> and <see cref="ICloneable"/> (although last needs to be overridden as will throw 
    /// <see cref="NotSupportedException"/>). The other capabilities are internally implemented by using the <see cref="GetPropertyValues"/> which must return a <see cref="PropertyValue{T}"/> for each and every updateable property. 
    /// <para>Generally, it is expected that the <see cref="EntityBase{TSelf}"/> is used as the base as it encapsulates this and adds further equality checking, and overrides <see cref="Clone"/>.</para>
    /// </remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBase : EntityCore, ICleanUp, IInitial, ICloneable, ICopyFrom
    {
        /// <summary>
        /// Copies (<see cref="ICopyFrom.CopyFrom(object?)"/>) or clones (<see cref="ICloneable.Clone"/>) the <paramref name="from"/> value.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <param name="from">The from value.</param>
        /// <param name="to">The to value (required to support a <see cref="ICopyFrom.CopyFrom(object?)"/>).</param>
        /// <returns>The resulting to value.</returns>
        protected internal static T? CopyOrClone<T>(T? from, T? to)
        {
            if (from == null)
                return default!;

            if (to == null && from is ICloneable c)
                return (T)c.Clone();
            else if (to is ICopyFrom cf)
            {
                cf.CopyFrom(from);
                return to;
            }
            else if (from is ICloneable c2)
                return (T)c2.Clone();

            return from;
        }

        /// <summary>
        /// Creates a clone of <see cref="Type"/> <typeparamref name="T"/> by instantiating a new instance and performing a <see cref="ICopyFrom.CopyFrom(object?)"/> from the <paramref name="from"/> value.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="from">The value to copy from.</param>
        /// <returns>A new cloned instance.</returns>
        protected static T CreateClone<T>(T from) where T : ICopyFrom, new()
        {
            var clone = new T();
            clone.CopyFrom(from);
            return clone;
        }

        /// <summary>
        /// Creates a new <see cref="PropertyValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The property <see cref="Type"/>.</typeparam>
        /// <param name="value">The property value.</param>
        /// <param name="setValue">The action to set (override) the value with the specified value.</param>
        /// <param name="defaultValue">The optional default value.</param>
        /// <returns>The <see cref="PropertyValue{T}"/>.</returns>
        protected static PropertyValue<T> CreateProperty<T>(T value, Action<T?> setValue, T? defaultValue = default) => new(value, setValue, defaultValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityBase"/> class.
        /// </summary>
        internal EntityBase() { }

        /// <inheritdoc/>
        [JsonIgnore]
        public virtual bool IsInitial => !GetPropertyValues().Any(x => !x.IsInitial);

        /// <summary>
        /// Gets all the property values (<see cref="IPropertyValue"/>) for the entity.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> including all properties.</returns>
        /// <remarks>Used to enable additional capabilities such as <see cref="CleanUp"/>, <see cref="IsInitial"/>, <see cref="object.GetHashCode"/>, <see cref="object.Equals(object)"/>, <see cref="CopyFrom(object?)"/>,
        /// <see cref="OnAcceptChanges"/> and <see cref="OnMakeReadOnly"/>.</remarks>
        protected abstract IEnumerable<IPropertyValue> GetPropertyValues();

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            GetPropertyValues().ForEach(pv => hash.Add(pv.GetHashCode()));
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object? other)
        {
            if (other is not EntityBase otherv)
                return false;
            else if (ReferenceEquals(this, other))
                return true;
            else if (GetType() != other.GetType())
                return false;

            var el = GetPropertyValues().GetEnumerator();
            var er = otherv.GetPropertyValues().GetEnumerator();
            while (el.MoveNext())
            {
                er.MoveNext();
                if (!el.Current.AreEqual(er.Current))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="other">The object to copy from.</param>
        public virtual void CopyFrom(object? other)
        {
            if (other is not EntityBase otherv)
                throw new ArgumentException($"Other value must be the same type: {GetType().FullName}.", nameof(other));
            else if (ReferenceEquals(this, other))
                return;
            else if (GetType() != other.GetType())
                throw new ArgumentException($"Other value must be the same type: {GetType().FullName}.", nameof(other));

            var el = GetPropertyValues().GetEnumerator();
            var er = otherv.GetPropertyValues().GetEnumerator();
            while (el.MoveNext())
            {
                er.MoveNext();
                el.Current.CopyFrom(er.Current);
            }
        }

        /// <inheritdoc/>
        public virtual void CleanUp() => GetPropertyValues().ForEach(pv => pv.Clean());

        /// <inheritdoc/>
        public override string ToString()
        {
            if (this is IIdentifier ii)
                return $"{base.ToString()} Id={ii.Id}";
            else if (this is IPrimaryKey pk)
                return $"{base.ToString()} PrimaryKey={pk.PrimaryKey}";
            else if (this is IEntityKey ek)
                return $"{base.ToString()} EntityKey={ek.EntityKey}";
            else
                return base.ToString();
        }

        /// <inheritdoc/>
        protected override void OnAcceptChanges()
        {
            foreach (var pv in GetPropertyValues())
            {
                if (pv.Value is EntityCore ec)
                    ec.AcceptChanges();
            }
        }

        /// <inheritdoc/>
        protected override void OnMakeReadOnly()
        {
            foreach (var pv in GetPropertyValues())
            {
                if (pv.Value is EntityCore ec)
                    ec.MakeReadOnly();
            }
        }

        /// <inheritdoc/>
        public virtual object Clone() => throw new NotSupportedException();
    }
}