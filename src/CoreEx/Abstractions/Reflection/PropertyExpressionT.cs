// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.Localization;
using CoreEx.RefData;
using System;
using System.Collections.Concurrent;
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
    /// <remarks>The compiled expression is cached so all subsequent requests for the same <typeparamref name="TEntity"/> and <typeparamref name="TProperty"/> is optimised for performance.</remarks>
    public class PropertyExpression<TEntity, TProperty> : IPropertyExpression
    {
        private struct ExpressionKey
        {
            public Type Type;
            public string Name;
        }

        private static readonly ConcurrentDictionary<ExpressionKey, PropertyExpression<TEntity, TProperty>> _expressions = new();

        private readonly Func<TEntity, TProperty> _getValue;

        /// <summary>
        /// Validates, creates and compiles the property expression; whilst also determinig the property friendly <see cref="Text"/>.
        /// </summary>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="JsonSerializer.Default"/> where not specified.</param>
        /// <returns>A <see cref="PropertyExpression{TEntity, TProperty}"/> which contains (in order) the compiled <see cref="System.Func{TEntity, TProperty}"/>, member name and resulting property text.</returns>
        internal static PropertyExpression<TEntity, TProperty> CreateInternal(Expression<Func<TEntity, TProperty>> propertyExpression, IJsonSerializer? jsonSerializer = null)
        {
            if ((propertyExpression ?? throw new ArgumentNullException(nameof(propertyExpression))).Body.NodeType != ExpressionType.MemberAccess)
                throw new InvalidOperationException("Only Member access expressions are supported.");

            var me = (MemberExpression)propertyExpression.Body;

            // Check cache and reuse as this is a *really* expensive operation.
            var key = new ExpressionKey { Type = me.Member.DeclaringType, Name = me.Member.Name };
            return _expressions.GetOrAdd(key, _ =>
            {
                if (me.Member.MemberType != MemberTypes.Property)
                    throw new InvalidOperationException("Expression results in a Member that is not a Property.");

                if (!me.Member.DeclaringType.GetTypeInfo().IsAssignableFrom(typeof(TEntity).GetTypeInfo()))
                    throw new InvalidOperationException("Expression results in a Member for a different Entity class.");

                string name = me.Member.Name;

                // Get the JSON property name (where configured).
                var js = jsonSerializer ?? JsonSerializer.Default;
                var isSerializable = js.TryGetJsonName(me.Member, out var jn);
                if (!isSerializable)
                {
                    // Probe corresponding 'Sid' or 'Sids' properties (using the standardised naming convention) where IReferenceData Type.
                    if (me.Member is PropertyInfo rpi && rpi.PropertyType.IsClass && rpi.PropertyType.GetInterfaces().Contains(typeof(IReferenceData)))
                    {
                        // Probe corresponding 'Sid' or 'Sids' properties for value (using the standardised naming convention).
                        var pi = me.Member.DeclaringType.GetProperty($"{name}Sid");
                        if (pi == null)
                            pi = me.Member.DeclaringType.GetProperty($"{name}Sids");

                        if (pi != null)
                            js.TryGetJsonName(pi, out jn);
                    }
                }

                // Either get the friendly text from a corresponding DisplayTextAttribute or split the PascalCase member name into friendlier sentence case text.
                DisplayAttribute ca = me.Member.GetCustomAttribute<DisplayAttribute>(true);

                // Create expression (with compilation also).
                return new PropertyExpression<TEntity, TProperty>(name, jn, ca?.Name == null ? me.Member.Name.ToSentenceCase() : ca.Name, isSerializable, propertyExpression.Compile());
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyExpression"/> class.
        /// </summary>
        private PropertyExpression(string name, string? jsonName, string text, bool isSerializable, Func<TEntity, TProperty> func)
        {
            Name = name;
            JsonName = jsonName;
            Text = text;
            IsJsonSerializable = isSerializable;
            _getValue = func;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public string? JsonName { get; }

        /// <inheritdoc/>
        public LText Text { get; }

        /// <inheritdoc/>
        public bool IsJsonSerializable { get;  }

        /// <inheritdoc/>
        object? IPropertyExpression.GetValue(object entity) => GetValue((TEntity)entity);

        /// <summary>
        /// Gets the property value for the given entity.
        /// </summary>
        /// <param name="entity">The entity value.</param>
        /// <returns>The corresponding property value.</returns>
        public TProperty? GetValue(TEntity entity) => entity == null ? default : _getValue.Invoke(entity);
    }
}