// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.Localization;
using CoreEx.RefData;
using Microsoft.Extensions.Caching.Memory;
using System;
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
    /// <remarks>The internal reflection comes at a performance cost; as such the resulting <see cref="PropertyExpression{TEntity, TProperty}"/> is cached using an <see cref="IMemoryCache"/>. The <see cref="PropertyExpression.AbsoluteExpirationTimespan"/>
    /// and <see cref="PropertyExpression.SlidingExpirationTimespan"/> enable additional basic policy configuration for the cached items.</remarks>
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
            if (propertyExpression.ThrowIfNull(nameof(propertyExpression)).Body.NodeType != ExpressionType.MemberAccess)
                throw new InvalidOperationException("Only Member access expressions are supported.");

            var cache = PropertyExpression.Cache;
            var me = (MemberExpression)propertyExpression.Body;

            // Check cache and reuse as this is a *really* expensive operation. Key contains: Entity type, property name, and json serializer (in case configuration is different).
            return cache.GetOrCreate((typeof(TEntity), me.Member.Name, jsonSerializer.GetType()), ce =>
            {
                if (PropertyExpression.AbsoluteExpirationTimespan.HasValue)
                    ce.SetAbsoluteExpiration(PropertyExpression.AbsoluteExpirationTimespan.Value);

                if (PropertyExpression.SlidingExpirationTimespan.HasValue)
                    ce.SetSlidingExpiration(PropertyExpression.SlidingExpirationTimespan.Value);

                if (me.Member.MemberType != MemberTypes.Property)
                    throw new InvalidOperationException("Expression results in a Member that is not a Property.");

                if (!me.Member.DeclaringType!.GetTypeInfo().IsAssignableFrom(typeof(TEntity).GetTypeInfo()))
                    throw new InvalidOperationException("Expression results in a Member for a different Entity class.");

                string name = me.Member.Name;

                // Get the JSON property name (where configured).
                var isSerializable = jsonSerializer.TryGetJsonName(me.Member, out var jn);
                if (!isSerializable)
                {
                    // Probe corresponding 'Sid' or 'Sids' properties (using the standardised naming convention) where IReferenceData Type.
                    if (me.Member is PropertyInfo rpi && rpi.PropertyType.IsClass && rpi.PropertyType.GetInterfaces().Contains(typeof(IReferenceData)))
                    {
                        var spi = me.Member.DeclaringType!.GetProperty($"{name}Sid") ?? me.Member.DeclaringType.GetProperty($"{name}Sids");
                        if (spi != null)
                            jsonSerializer.TryGetJsonName(spi, out jn);
                    }
                }

                // Either get the friendly text from a corresponding DisplayAttribute or split the member name into friendlier sentence case text.
                DisplayAttribute? ca = me.Member.GetCustomAttribute<DisplayAttribute>(true);

                // Create a setter from the getter.
                var pi = (PropertyInfo)me.Member;
                Action<TEntity, TProperty>? setValue = null;
                if (pi.CanWrite)
                {
                    var pte = Expression.Parameter(typeof(TEntity), "e");
                    var ptp = Expression.Parameter(typeof(TProperty), "p");
                    var exp = Expression.Lambda<Action<TEntity, TProperty>>(Expression.Call(pte, pi.GetSetMethod()!, ptp), pte, ptp);
                    setValue = exp.Compile();
                }

                // Create expression (with compilation also).
                return new PropertyExpression<TEntity, TProperty>(pi, name, jn, ca?.Name, isSerializable, propertyExpression.Compile(), setValue);
            })!;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyExpression"/> class.
        /// </summary>
        private PropertyExpression(PropertyInfo pi, string name, string? jsonName, string? text, bool isSerializable, Func<TEntity, TProperty> getValue, Action<TEntity, TProperty>? setValue)
        {
            PropertyInfo = pi;
            Name = name;
            JsonName = jsonName;
            Text = PropertyExpression.CreatePropertyLText(typeof(TEntity), pi, text ?? Name.ToSentenceCase());
            IsJsonSerializable = isSerializable;
            IsClass = PropertyInfo.PropertyType.IsClass && PropertyInfo.PropertyType != typeof(string);
            _getValue = getValue;
            _setValue = setValue;
        }

        /// <inheritdoc/>
        public PropertyInfo PropertyInfo { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string? JsonName { get; }

        /// <inheritdoc/>
        public LText Text { get; }

        /// <inheritdoc/>
        public bool IsJsonSerializable { get;  }

        /// <inheritdoc/>
        public bool IsClass { get; }

        /// <inheritdoc/>
        object? IPropertyExpression.GetDefault() => default;

        /// <summary>
        /// Gets the default value.
        /// </summary>
        /// <returns></returns>
        public TProperty? GetDefault() => default;

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

            _setValue(entity.ThrowIfNull(nameof(entity)), value!);
        }
    }
}