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
}