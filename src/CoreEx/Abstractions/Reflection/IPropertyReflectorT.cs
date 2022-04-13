// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables a reflector for a given <typeparamref name="TEntity"/> property.
    /// </summary>
    /// <typeparam name="TEntity">The entity (class) <see cref="Type"/>.</typeparam>
    public interface IPropertyReflector<TEntity> : IPropertyReflector where TEntity : class, new()
    {
        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="entity">The entity whose value is to be set.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> where the value was changed; otherwise, <c>false</c> (i.e. same value).</returns>
        bool SetValue(TEntity entity, object? value);

        /// <summary>
        /// Creates a new instance and sets the property value.
        /// </summary>
        /// <param name="entity">The entity whose value is to be set.</param>
        /// <returns><c>true</c> where the value was changed; otherwise, <c>false</c> (i.e. same value).</returns>
        (bool changed, object? value) NewValue(TEntity entity);
    }
}