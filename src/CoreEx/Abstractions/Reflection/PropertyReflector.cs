// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides a reflector for a given entity property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
    public class PropertyReflector<TEntity, TProperty> : IPropertyReflector where TEntity : class
    {
        private readonly Lazy<Dictionary<string, object?>> _data = new(true);
        private IEntityReflector? _entityReflector;
        private IEntityReflector? _itemReflector;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyReflector{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="args">The <see cref="EntityReflectorArgs"/>.</param>
        /// <param name="propertyExpression">The <see cref="LambdaExpression"/> to reference the source entity property.</param>
        public PropertyReflector(EntityReflectorArgs args, Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));
            PropertyExpression = Reflection.PropertyExpression.Create(propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression)), args.JsonSerializer);
            IsClass = PropertyInfo.PropertyType.IsClass && PropertyInfo.PropertyType != typeof(string);
            TypeCode = IsClass ? TypeReflectorTypeCode.Complex : TypeReflectorTypeCode.Simple;
            IsEnumerable = PropertyInfo.PropertyType != typeof(string) && (PropertyInfo.PropertyType.IsArray || PropertyInfo.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)));
            if (IsEnumerable)
            {
                var tr = TypeReflector.GetCollectionItemType(Type);
                ItemType = tr.ItemType;
                TypeCode = tr.TypeCode;
                if (ItemType != null)
                    ItemTypeCode = TypeReflector.GetCollectionItemType(ItemType).TypeCode;
            }
        }

        /// <inheritdoc/>
        public string Name => PropertyExpression.Name;

        /// <inheritdoc/>
        public string? JsonName => PropertyExpression.JsonName;

        /// <inheritdoc/>
        public EntityReflectorArgs Args { get; }

        /// <inheritdoc/>
        public Dictionary<string, object?> Data { get => _data.Value; }

        /// <inheritdoc/>
        IPropertyExpression IPropertyReflector.PropertyExpression => PropertyExpression;

        /// <summary>
        /// Gets the compiled <see cref="PropertyExpression{TEntity, TProperty}"/>.
        /// </summary>
        public PropertyExpression<TEntity, TProperty> PropertyExpression { get; }

        /// <inheritdoc/>
        public PropertyInfo PropertyInfo => PropertyExpression.PropertyInfo;

        /// <inheritdoc/>
        public bool IsClass { get; }

        /// <inheritdoc/>
        public bool IsEnumerable { get; }

        /// <inheritdoc/>
        public Type EntityType => typeof(TEntity);

        /// <inheritdoc/>
        public Type Type => typeof(TProperty);

        /// <inheritdoc/>
        public TypeReflectorTypeCode TypeCode { get; }

        /// <inheritdoc/>
        public Type? ItemType { get; }

        /// <inheritdoc/>
        public TypeReflectorTypeCode? ItemTypeCode { get; }

        /// <inheritdoc/>
        public IEntityReflector? GetEntityReflector() => TypeCode == TypeReflectorTypeCode.Simple ? null : _entityReflector ??= EntityReflector.GetReflector(Args, Type);

        /// <inheritdoc/>
        public IEntityReflector? GetItemEntityReflector() => ItemTypeCode == TypeReflectorTypeCode.Simple ? null : _itemReflector ??= EntityReflector.GetReflector(Args, ItemType!);
    }
}