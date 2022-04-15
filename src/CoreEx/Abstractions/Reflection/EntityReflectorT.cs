// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides a reflector for a given entity (class) (<see cref="Type"/>).
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public class EntityReflector<TEntity> : IEntityReflector where TEntity : class
    {
        private readonly Dictionary<string, IPropertyReflector> _properties;
        private readonly Dictionary<string, IPropertyReflector> _jsonProperties;
        private readonly Lazy<Dictionary<string, object?>> _data = new(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityReflector{TEntity}"/> class.
        /// </summary>
        /// <param name="args">The <see cref="EntityReflectorArgs"/>.</param>
        internal EntityReflector(EntityReflectorArgs? args = null)
        {
            Args = args ?? new EntityReflectorArgs();
            _properties = new Dictionary<string, IPropertyReflector>(StringComparer.Ordinal);
            _jsonProperties = new Dictionary<string, IPropertyReflector>(Args.NameComparer ?? StringComparer.OrdinalIgnoreCase);

            if (!Args.AutoPopulateProperties)
                return;

            var pe = Expression.Parameter(typeof(TEntity), "x");

            foreach (var p in EntityReflector.GetProperties(typeof(TEntity)))
            {
                var lex = Expression.Lambda(Expression.Property(pe, p.Name), pe);
                var pr = (IPropertyReflector)Activator.CreateInstance(typeof(PropertyReflector<,>).MakeGenericType(typeof(TEntity), p.PropertyType), Args, lex);

                if (Args.PropertyBuilder != null && !Args.PropertyBuilder(pr))
                    continue;

                AddProperty(pr);
            }
        }

        /// <summary>
        /// Gets the <see cref="EntityReflectorArgs"/>.
        /// </summary>
        public EntityReflectorArgs Args { get; private set; }

        /// <summary>
        /// Gets the entity <see cref="Type"/>.
        /// </summary>
        public Type Type => typeof(TEntity);

        /// <summary>
        /// Gets the <see cref="Dictionary{TKey, TValue}"/> for storing additional data.
        /// </summary>
        public Dictionary<string, object?> Data { get => _data.Value; }

        /// <summary>
        /// Adds a <see cref="PropertyReflector{TEntity, TProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <returns>The <see cref="PropertyReflector{TEntity, TProperty}"/>.</returns>
        public PropertyReflector<TEntity, TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            var pr = new PropertyReflector<TEntity, TProperty>(Args, propertyExpression);
            AddProperty(pr);
            return pr;
        }

        /// <summary>
        /// Adds the <see cref="IPropertyReflector"/> to the underlying property collections.
        /// </summary>
        private void AddProperty(IPropertyReflector propertyReflector)
        {
            if (propertyReflector == null)
                throw new ArgumentNullException(nameof(propertyReflector));

            if (_properties.ContainsKey(propertyReflector.Name))
                throw new ArgumentException($"Property with name '{propertyReflector.Name}' can not be specified more than once.", nameof(propertyReflector));

            if (propertyReflector.PropertyExpression.IsJsonSerializable && propertyReflector.JsonName != null)
            {
                if (_jsonProperties.ContainsKey(propertyReflector.JsonName))
                    throw new ArgumentException($"Property with name '{propertyReflector.JsonName}' can not be specified more than once.", nameof(propertyReflector));

                _jsonProperties.Add(propertyReflector.JsonName, propertyReflector);
            }

            _properties.Add(propertyReflector.Name, propertyReflector);
        }

        /// <inheritdoc/>
        public IPropertyReflector GetProperty(string name)
        {
            _properties.TryGetValue(name, out var value);
            return value;
        }

        /// <inheritdoc/>
        public IPropertyReflector? GetJsonProperty(string jsonName)
        {
            _jsonProperties.TryGetValue(jsonName, out var value);
            return value;
        }

        /// <summary>
        /// Gets all the properties.
        /// </summary>
        public IReadOnlyCollection<IPropertyReflector> GetProperties() => new ReadOnlyCollection<IPropertyReflector>(_properties.Values.OfType<IPropertyReflector>().ToList());
    }
}