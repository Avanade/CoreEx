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
    /// Provides a reflector for a given <see cref="Type"/> property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
    public class PropertyReflector<TEntity, TProperty> : IPropertyReflector
    {
        private readonly Lazy<Dictionary<string, object?>> _data = new(true);
        private ITypeReflector? _typeReflector;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyReflector{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="args">The <see cref="TypeReflectorArgs"/>.</param>
        /// <param name="propertyExpression">The <see cref="LambdaExpression"/> to reference the source entity property.</param>
        public PropertyReflector(TypeReflectorArgs args, Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));
            PropertyExpression = Reflection.PropertyExpression.Create(propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression)), args.JsonSerializer);
            IsClass = PropertyInfo.PropertyType.IsClass && PropertyInfo.PropertyType != typeof(string);
            TypeCode = IsClass ? TypeReflectorTypeCode.Complex : TypeReflectorTypeCode.Simple;
            IsEnumerable = IsClass && (PropertyInfo.PropertyType.IsArray || PropertyInfo.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)));
            if (IsEnumerable)
            {
                _typeReflector = TypeReflector.GetReflector(Args, Type);
                TypeCode = _typeReflector!.TypeCode;
            }
        }

        /// <inheritdoc/>
        public string Name => PropertyExpression.Name;

        /// <inheritdoc/>
        public string? JsonName => PropertyExpression.JsonName;

        /// <inheritdoc/>
        public TypeReflectorArgs Args { get; }

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
        public ITypeReflector? GetTypeReflector() => _typeReflector ??= TypeReflector.GetReflector(Args, Type);

        /// <inheritdoc/>
        bool IPropertyReflector.Compare(object? x, object? y) => Compare((TProperty)(x ?? default(TProperty)!), (TProperty)(y ?? default(TProperty)!));

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        public bool Compare(TProperty? x, TProperty? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            var left = x == null ? y : x;
            var right = x == null ? x : y;
            if (left == null || right == null)
                return false;

            if (left is IEquatable<TProperty> eq)
                return eq.Equals(right!);
            else if (IsEnumerable)
                return GetTypeReflector()!.Compare(x, y);
            else
                return EqualityComparer<TProperty>.Default.Equals(left, right);
        }
    }
}