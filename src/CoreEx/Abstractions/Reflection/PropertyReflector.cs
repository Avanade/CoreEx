// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides a reflector for a given entity property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class PropertyReflector<TEntity, TProperty> : IPropertyReflector<TEntity> where TEntity : class, new()
    {
        private readonly Lazy<Dictionary<string, object>> _data = new(true);
        private readonly bool _newAsDefault = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyReflector{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="args">The <see cref="EntityReflectorArgs"/>.</param>
        /// <param name="propertyExpression">The <see cref="LambdaExpression"/> to reference the source entity property.</param>
        public PropertyReflector(EntityReflectorArgs args, Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));
            PropertyExpression = Reflection.PropertyExpression.Create(propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression)));
            PropertyInfo = EntityReflector.GetPropertyInfo(typeof(TEntity), PropertyName) ?? throw new ArgumentException($"Propery '{PropertyName}' does not exist for Type.", nameof(propertyExpression));

            if (PropertyInfo.PropertyType.IsClass && PropertyInfo.PropertyType != typeof(string))
                EntityCollectionReflector = TypeReflector.Create(PropertyInfo);

            if (PropertyInfo.PropertyType.IsValueType || (PropertyInfo.PropertyType.IsClass && PropertyInfo.PropertyType == typeof(string)))
                _newAsDefault = true;
        }

        /// <inheritdoc/>
        public EntityReflectorArgs Args { get; }

        /// <inheritdoc/>
        public Dictionary<string, object> Data { get => _data.Value; }

        /// <inheritdoc/>
        IPropertyExpression IPropertyReflector.PropertyExpression => PropertyExpression;

        /// <summary>
        /// Gets the compiled <see cref="PropertyExpression{TEntity, TProperty}"/>.
        /// </summary>
        public PropertyExpression<TEntity, TProperty> PropertyExpression { get; }

        /// <inheritdoc/>
        public TypeReflector? EntityCollectionReflector { get; }

        /// <inheritdoc/>
        public bool IsEntityOrCollection => EntityCollectionReflector != null;

        /// <inheritdoc/>
        public PropertyInfo PropertyInfo { get; }

        /// <inheritdoc/>
        public string PropertyName => PropertyExpression.Name;

        /// <inheritdoc/>
        public string? JsonName => PropertyExpression.JsonName;

        /// <inheritdoc/>
        public Type EntityType => typeof(TEntity);

        /// <inheritdoc/>
        public Type PropertyType => typeof(TProperty);

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for the property where the <see cref="EntityCollectionReflector"/> <see cref="TypeReflector.TypeCode"/> is <see cref="TypeReflectorTypeCode.Complex"/>; otherwise, <c>null</c>.
        /// </summary>
        /// <returns>An <see cref="IEntityReflector"/>.</returns>
        public IEntityReflector? GetEntityReflector()
            => !IsEntityOrCollection || EntityCollectionReflector!.TypeCode != TypeReflectorTypeCode.Complex ? null : EntityReflector.GetReflector(Args, PropertyType);

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for the collection item property; will return <c>null</c> where <see cref="TypeReflector.CollectionItemType"/> is not a class.
        /// </summary>
        /// <returns>An <see cref="IEntityReflector"/>.</returns>
        public IEntityReflector? GetItemEntityReflector()
        {
            if (!IsEntityOrCollection || EntityCollectionReflector!.TypeCode == TypeReflectorTypeCode.Complex)
                return null;

            if (EntityCollectionReflector.CollectionItemType.IsClass && EntityCollectionReflector.CollectionItemType == typeof(string))
                return null;

            return EntityReflector.GetReflector(Args, EntityCollectionReflector.CollectionItemType);
        }

        /// <inheritdoc/>
        bool IPropertyReflector.SetValue(object entity, object? value) => SetValue((TEntity)entity, value == null ? default! : (TProperty)value!);

        /// <inheritdoc/>
        bool IPropertyReflector<TEntity>.SetValue(TEntity entity, object? value) => SetValue(entity, value == null ? default! : (TProperty)value!);

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="entity">The entity whose value is to be set.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> where the value was changed; otherwise, <c>false</c> (i.e. same value).</returns>
        public bool SetValue(TEntity entity, TProperty? value)
        {
            var existing = PropertyExpression.GetValue(entity);
            if (Equals(existing, value))
                return false;

            PropertyInfo.SetValue(entity, value);
            return true;
        }

        /// <inheritdoc/>
        (bool changed, object? value) IPropertyReflector.NewValue(object entity) => NewValue((TEntity)entity);

        /// <summary>
        /// Creates a new instance and sets the property value.
        /// </summary>
        /// <param name="entity">The entity whose value is to be set.</param>
        /// <returns><c>true</c> where the value was changed; otherwise, <c>false</c> (i.e. same value).</returns>
        public (bool changed, object? value) NewValue(TEntity entity)
        {
            var val = NewValue();
            return (SetValue(entity, val), val);
        }

        /// <inheritdoc/>
        object? IPropertyReflector.NewValue() => NewValue();

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <returns>The value.</returns>
        public TProperty? NewValue() => _newAsDefault ? default! : 
            (IsEntityOrCollection && EntityCollectionReflector!.IsCollectionType) ? (TProperty)EntityCollectionReflector.CreateCollectionValue()! : Activator.CreateInstance<TProperty>();
    }
}