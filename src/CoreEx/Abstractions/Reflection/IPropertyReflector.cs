// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables a reflector for a given entity (class) property.
    /// </summary>
    public interface IPropertyReflector
    {
        /// <summary>
        /// Gets the <see cref="EntityReflectorArgs"/>.
        /// </summary>
        EntityReflectorArgs Args { get; }

        /// <summary>
        /// Gets the <see cref="Dictionary{TKey, TValue}"/> for storing additional data.
        /// </summary>
        Dictionary<string, object> Data { get; }

        /// <summary>
        /// Gets the compiled <see cref="IPropertyExpression"/>.
        /// </summary>
        IPropertyExpression PropertyExpression { get; }

        /// <summary>
        /// Gets the <see cref="TypeReflector"/> (only set where the property <see cref="IsEntityOrCollection"/>).
        /// </summary>
        TypeReflector? EntityCollectionReflector { get; }

        /// <summary>
        /// Indicates whether the property is an entity (class) or collection (see <see cref="EntityCollectionReflector"/>).
        /// </summary>
        bool IsEntityOrCollection { get; }

        /// <summary>
        /// Gets the corresponding <see cref="PropertyInfo"/>;
        /// </summary>
        PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        string? JsonName { get; }

        /// <summary>
        /// Gets the parent entity <see cref="Type"/>.
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets the property <see cref="Type"/>.
        /// </summary>
        Type PropertyType { get; }

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for the property; will return <c>null</c> where <see cref="PropertyType"/> is not a class.
        /// </summary>
        /// <returns>An <see cref="IEntityReflector"/>.</returns>
        IEntityReflector? GetEntityReflector();

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for the collection item property; will return <c>null</c> where <see cref="TypeReflector.CollectionItemType"/> is not a class.
        /// </summary>
        /// <returns>An <see cref="IEntityReflector"/>.</returns>
        IEntityReflector? GetItemEntityReflector();

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="entity">The entity whose value is to be set.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> where the value was changed; otherwise, <c>false</c> (i.e. same value).</returns>
        bool SetValue(object entity, object? value);

        /// <summary>
        /// Creates a new instance (value) and sets the property value.
        /// </summary>
        /// <param name="entity">The entity whose value is to be set.</param>
        /// <returns><c>true</c> where the value was changed; otherwise, <c>false</c> (i.e. same value).</returns>
        (bool changed, object? value) NewValue(object entity);

        /// <summary>
        /// Creates a new instance (value).
        /// </summary>
        /// <returns>The value.</returns>
        object? NewValue();
    }
}