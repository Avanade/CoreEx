﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides the property value capabilities enabled by <see cref="EntityBase.GetPropertyValues"/>.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="setValue">The action to set (override) the value with the specified value.</param>
    /// <param name="defaultValue">The optional default value override.</param>
    public struct PropertyValue<T>(string name, T value, Action<T?> setValue, T? defaultValue = default) : IPropertyValue
    {
        private readonly Action<T?> _setValue = setValue;

        /// <inheritdoc/>
        public string Name { get; } = name.ThrowIfNullOrEmpty(nameof(name));

        /// <inheritdoc/>
        readonly object? IPropertyValue.Value => Value;

        /// <summary>
        /// Gets the property value.
        /// </summary>
        public T? Value { get; private set; } = value;

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public T? DefaultValue { get; } = defaultValue ?? default;

        /// <inheritdoc/>
        public void Clean() => SetValue(Cleaner.Clean(Value));

        /// <inheritdoc/>
        public readonly bool IsInitial => Cleaner.IsDefault(Value, DefaultValue);

        /// <inheritdoc/>
        void IPropertyValue.SetValue(object? value) => SetValue((T)value!);

        /// <summary>
        /// Sets (override) the underlying <see cref="Value"/> with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The overridding value.</param>
        public void SetValue(T? value)
        {
            _setValue(value); 
            Value = value;
        }

        /// <inheritdoc/>
        readonly bool IPropertyValue.AreEqual(IPropertyValue value) => AreEqual((PropertyValue<T>)value!);

        /// <summary>
        /// Indicates whether the other <paramref name="propertyValue"/> is equal to this.
        /// </summary>
        /// <param name="propertyValue">The value to compare to.</param>
        /// <returns><c>true</c> indicates they are equal; otherwise, <c>false</c> for not equal.</returns>
        public readonly bool AreEqual(PropertyValue<T> propertyValue) => Value == null && propertyValue.Value == null || (Value == null ? propertyValue.Value!.Equals(Value) : Value.Equals(propertyValue.Value));

        /// <inheritdoc/>
        public override readonly int GetHashCode() => Value?.GetHashCode() ?? 0;

        /// <summary>
        /// Performs a copy or clone from the other <paramref name="propertyValue"/>.
        /// </summary>
        /// <param name="propertyValue">The <see cref="IPropertyValue"/> to copy or clone from.</param>
        void IPropertyValue.CopyFrom(IPropertyValue propertyValue) => CopyFrom((PropertyValue<T>)propertyValue);

        /// <inheritdoc/>
        void CopyFrom(PropertyValue<T> propertyValue)
        {
            if (propertyValue.Value is null || Comparer<T>.Default.Compare(propertyValue.Value, default!) == 0)
            {
                SetValue(default);
                return;
            }

            if (propertyValue.Value is string s)
            {
                SetValue((T)(object)s);
                return;
            }

            if (propertyValue is ICloneable clone)
            {
                SetValue((T)clone);
                return;
            }

            if (propertyValue.Value is EntityBase eb)
            {
                var v = Activator.CreateInstance<T>() as EntityBase;
                v!.CopyFrom(eb);
                SetValue((T)(object)v);
                return;
            }

            if (propertyValue.Value is IEntityBaseCollection ebc)
            {
                SetValue((T)ebc.Clone());
                return;
            }

            SetValue(propertyValue.Value);
        }
    }
}