// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides the base capabilities for an entended entity.
    /// </summary>
    /// <remarks>Inherits from <see cref="EntityCore"/> and implements <see cref="ICleanUp"/>, <see cref="IInitial"/> and <see cref="ICopyFrom"/>. These additional capabilities are internally implemented by using the <see cref="GetPropertyValues"/>
    /// which must return a <see cref="PropertyValue{T}"/> for each and every updateable property. 
    /// </remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBase : EntityCore, ICleanUp, IInitial, ICopyFrom
    {
        /// <summary>
        /// Creates a new <see cref="PropertyValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The property <see cref="Type"/>.</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="setValue">The action to set (override) the value with the specified value.</param>
        /// <param name="defaultValue">The optional default value.</param>
        /// <returns>The <see cref="PropertyValue{T}"/>.</returns>
        protected static PropertyValue<T> CreateProperty<T>(string name, T value, Action<T?> setValue, T? defaultValue = default) => new(name,value, setValue, defaultValue);

        /// <inheritdoc/>
        [JsonIgnore]
        public virtual bool IsInitial => !GetPropertyValues().Any(x => !x.IsInitial);

        /// <summary>
        /// Gets all the property values (<see cref="IPropertyValue"/>) for the entity.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> for all properties.</returns>
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
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLogEx"/> A.</param>
        /// <param name="b"><see cref="ChangeLogEx"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(EntityBase? a, EntityBase? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLogEx"/> A.</param>
        /// <param name="b"><see cref="ChangeLogEx"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(EntityBase? a, EntityBase? b) => !Equals(a, b);

        /// <summary>
        /// Performs a deep copy from another object updating this instance.
        /// </summary>
        /// <param name="other">The object to copy from.</param>
        public virtual void CopyFrom(object? other)
        {
            if (ReferenceEquals(this, other))
                return;

            if (other is EntityBase otherv)
            {
                var t = GetType();
                var to = other.GetType();
                if (t == other.GetType())
                {
                    var el = GetPropertyValues().GetEnumerator();
                    var er = otherv.GetPropertyValues().GetEnumerator();
                    while (el.MoveNext())
                    {
                        er.MoveNext();
                        el.Current.CopyFrom(er.Current);
                    }

                    return;
                }

                if (t.IsAssignableFrom(to) || t.IsSubclassOf(to))
                {
                    var el = GetPropertyValues().GetEnumerator();
                    var er = otherv.GetPropertyValues().GetEnumerator();

                    var opvc = new PropertyValueCollection();
                    while (er.MoveNext()) { opvc.Add(er.Current); }

                    while (el.MoveNext())
                    {
                        if (opvc.TryGetValue(el.Current.Name, out var opv))
                            el.Current.CopyFrom(opv);
                    }

                    return;
                }
            }

            throw new ArgumentException($"Other value must be the same type or is assignable/subclass from: {GetType().FullName}.", nameof(other));
        }

        private class PropertyValueCollection : KeyedCollection<string, IPropertyValue>
        {
            protected override string GetKeyForItem(IPropertyValue item) => item.Name;
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
                return base.ToString()!;
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
    }
}