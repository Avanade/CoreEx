// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Defines the property value capabilities enabled by <see cref="EntityBase.GetPropertyValues"/>.
    /// </summary>
    public interface IPropertyValue
    {
        /// <summary>
        /// Gets the property value.
        /// </summary>
        object? Value { get; }

        /// <summary>
        /// Indicates whether the value is considered the initial value.
        /// </summary>
        bool IsInitial { get; }

        /// <summary>
        /// Cleans a value and overrides the value with <c>null</c> when the value is <see cref="IInitial.IsInitial"/>.
        /// </summary>
        public void Clean();

        /// <summary>
        /// Sets (overrides) the underlying <see cref="Value"/> with the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The overridding value.</param>
        /// <remarks>This is needed to support <see cref="ICopyFrom"/> and <see cref="ICloneable"/> functionality.</remarks>
        void SetValue(object? value);

        /// <summary>
        /// Indicates whether the other <paramref name="propertyValue"/> is equal to this.
        /// </summary>
        /// <param name="propertyValue">The <see cref="IPropertyValue"/> to compare to.</param>
        /// <returns><c>true</c> indicates they are equal; otherwise, <c>false</c> for not equal.</returns>
        bool AreEqual(IPropertyValue propertyValue);

        /// <summary>
        /// Gets the hash code for the <see cref="Value"/>.
        /// </summary>
        /// <returns>The hash code for the <see cref="Value"/>.</returns>
        int GetHashCode();

        /// <summary>
        /// Performs a copy or clone from the other <paramref name="propertyValue"/>.
        /// </summary>
        /// <param name="propertyValue">The <see cref="IPropertyValue"/> to copy or clone from.</param>
        void CopyFrom(IPropertyValue propertyValue);
    }

    /// <summary>
    /// Provides the property value capabilities enabled by <see cref="EntityBase.GetPropertyValues"/>.
    /// </summary>
    public struct PropertyValue<T> : IPropertyValue
    {
        private readonly Action<T?> _setValue;

        /// <summary>
        /// Initializes a new instance of the 
        /// </summary>
        /// <param name="value">The property value.</param>
        /// <param name="setValue">The action to set (override) the value with the specified value.</param>
        /// <param name="defaultValue">The optional default value override.</param>
        public PropertyValue(T value, Action<T?> setValue, T? defaultValue = default)
        {
            Value = value;
            _setValue = setValue;
            DefaultValue = defaultValue ?? default;
        }

        /// <inheritdoc/>
        object? IPropertyValue.Value => Value;

        /// <summary>
        /// Gets the property value.
        /// </summary>
        public T? Value { get; private set; }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public T? DefaultValue { get; }

        /// <inheritdoc/>
        public void Clean() => SetValue(Cleaner.Clean(Value));

        /// <inheritdoc/>
        public bool IsInitial => Cleaner.IsDefault(Value, DefaultValue);

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
        bool IPropertyValue.AreEqual(IPropertyValue value) => AreEqual((PropertyValue<T>)value!);

        /// <summary>
        /// Indicates whether the other <paramref name="propertyValue"/> is equal to this.
        /// </summary>
        /// <param name="propertyValue">The value to compare to.</param>
        /// <returns><c>true</c> indicates they are equal; otherwise, <c>false</c> for not equal.</returns>
        public bool AreEqual(PropertyValue<T> propertyValue) => Value == null && propertyValue.Value == null || (Value == null ? propertyValue.Value!.Equals(Value) : Value.Equals(propertyValue.Value));

        /// <inheritdoc/>
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        /// <summary>
        /// Performs a copy or clone from the other <paramref name="propertyValue"/>.
        /// </summary>
        /// <param name="propertyValue">The <see cref="IPropertyValue"/> to copy or clone from.</param>
        void IPropertyValue.CopyFrom(IPropertyValue propertyValue) => CopyFrom((PropertyValue<T>)propertyValue);

        /// <inheritdoc/>
        void CopyFrom(PropertyValue<T> propertyValue) => SetValue(propertyValue.Value is EntityBase ? EntityBase.CopyOrClone(propertyValue.Value, Value) : propertyValue.Value);
    }
}