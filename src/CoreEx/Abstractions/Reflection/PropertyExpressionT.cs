// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.Localization;
using CoreEx.RefData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides property <see cref="Expression"/> capability.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <remarks>The internal reflection comes at a performance cost; the resulting <see cref="PropertyExpression{TEntity, TProperty}"/> should be cached and reused where possible.</remarks>
    public class PropertyExpression<TEntity, TProperty> : IPropertyExpression
    {
        private readonly Func<TEntity, TProperty> _getValue;
        private readonly Action<TEntity, TProperty>? _setValue;

        /// <summary>
        /// Validates, creates and compiles the property expression; whilst also determinig the property friendly <see cref="Text"/>.
        /// </summary>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>A <see cref="PropertyExpression{TEntity, TProperty}"/> which contains (in order) the compiled <see cref="System.Func{TEntity, TProperty}"/>, member name and resulting property text.</returns>
        internal static PropertyExpression<TEntity, TProperty> CreateInternal(Expression<Func<TEntity, TProperty>> propertyExpression, IJsonSerializer jsonSerializer)
        {
            if ((propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression))).Body.NodeType != ExpressionType.MemberAccess)
                throw new InvalidOperationException("Only Member access expressions are supported.");

            var me = (MemberExpression)propertyExpression.Body;

            if (me.Member.MemberType != MemberTypes.Property)
                throw new InvalidOperationException("Expression results in a Member that is not a Property.");

            if (!me.Member.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(TEntity).GetTypeInfo()))
                throw new InvalidOperationException("Expression results in a Member for a different Entity class.");

            string name = me.Member.Name;

            // Get the JSON property name (where configured).
            var isSerializable = jsonSerializer.TryGetJsonName(me.Member, out var jn);
            if (!isSerializable)
            {
                // Probe corresponding 'Sid' or 'Sids' properties (using the standardised naming convention) where IReferenceData Type.
                if (me.Member is PropertyInfo rpi && rpi.PropertyType.IsClass && rpi.PropertyType.GetInterfaces().Contains(typeof(IReferenceData)))
                {
                    var spi = me.Member.DeclaringType.GetProperty($"{name}Sid");
                    if (spi == null)
                        spi = me.Member.DeclaringType.GetProperty($"{name}Sids");

                    if (spi != null)
                        jsonSerializer.TryGetJsonName(spi, out jn);
                }
            }

            // Either get the friendly text from a corresponding DisplayAttribute or split the member name into friendlier sentence case text.
            DisplayAttribute ca = me.Member.GetCustomAttribute<DisplayAttribute>(true);

            // Create a setter from the getter.
            var pi = (PropertyInfo)me.Member;
            Action<TEntity, TProperty>? setValue = null;
            if (pi.CanWrite)
            {
                var pte = Expression.Parameter(typeof(TEntity), "e");
                var ptp = Expression.Parameter(typeof(TProperty), "p");
                var exp = Expression.Lambda<Action<TEntity, TProperty>>(Expression.Call(pte, pi.GetSetMethod(), ptp), pte, ptp);
                setValue = exp.Compile();
            }

            // Create expression (with compilation also).
            return new PropertyExpression<TEntity, TProperty>(pi, name, jn, ca?.Name == null ? me.Member.Name.ToSentenceCase() : ca.Name, isSerializable, propertyExpression.Compile(), setValue);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyExpression"/> class.
        /// </summary>
        private PropertyExpression(PropertyInfo pi, string name, string? jsonName, string text, bool isSerializable, Func<TEntity, TProperty> getValue, Action<TEntity, TProperty>? setValue)
        {
            PropertyInfo = pi;
            Name = name;
            JsonName = jsonName;
            Text = text;
            IsJsonSerializable = isSerializable;
            _getValue = getValue;
            _setValue = setValue;
            TypeReflector = pi.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)) ? TypeReflector.Create(pi) : null;
        }

        /// <inheritdoc/>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets the corresponding <see cref="TypeReflector"/> for <see cref="IEnumerable"/> types.
        /// </summary>
        public TypeReflector? TypeReflector { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string? JsonName { get; }

        /// <inheritdoc/>
        public LText Text { get; }

        /// <inheritdoc/>
        public bool IsJsonSerializable { get;  }

        /// <inheritdoc/>
        object? IPropertyExpression.GetDefault() => default;

        /// <inheritdoc/>
        bool IPropertyExpression.Compare(object? x, object? y) => Compare((TProperty)(x ?? default(TProperty)!), (TProperty)(y ?? default(TProperty)!));

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
            else if (TypeReflector != null)
                return TypeReflector.CompareSequence(x, y);
            else
                return EqualityComparer<TProperty>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        object? IPropertyExpression.GetValue(object? entity) => GetValue((TEntity)entity!);

        /// <summary>
        /// Gets the property value for the given entity.
        /// </summary>
        /// <param name="entity">The entity value.</param>
        /// <returns>The corresponding property value.</returns>
        public TProperty? GetValue(TEntity? entity) => entity == null ? default : _getValue.Invoke(entity);

        /// <inheritdoc/>
        void IPropertyExpression.SetValue(object entity, object? value) => SetValue((TEntity)entity, value == null ? default : (TProperty?)value);

        /// <summary>
        /// Sets the property value for the given entity.
        /// </summary>
        /// <param name="entity">The entity value.</param>
        /// <param name="value">The corresponding property value.</param>
        public void SetValue(TEntity entity, TProperty? value)
        {
            if (_setValue == null)
                throw new InvalidOperationException($"Property '{Name}' does not support a set (write) operation.");

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _setValue(entity, value!);
        }
    }
}