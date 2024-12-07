// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.Mapping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Provides mapping from a <typeparamref name="TSource"/> <see cref="Type"/> and <see cref="JsonObject"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    public class JsonObjectMapper<TSource> : IJsonObjectMapper<TSource>, IJsonObjectMapperMappings where TSource : class, new()
    {
        private readonly List<IPropertyJsonMapper> _mappings = [];
        private readonly bool _implementsIIdentifier = typeof(IIdentifier).IsAssignableFrom(typeof(TSource));

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObjectMapper{TSource}"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="Text.Json.JsonSerializer"/>; defaults where not specified.</param>
        /// <param name="autoMap">Indicates whether the entity should automatically map all public get/set properties, where the property and column names are all assumed to share the same name.</param>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        public JsonObjectMapper(Text.Json.JsonSerializer? jsonSerializer = null, bool autoMap = false, params string[] ignoreSrceProperties)
        {
            if (typeof(TSource) == typeof(string)) throw new InvalidOperationException("TSource must not be a String.");

            JsonSerializer = jsonSerializer ?? (CoreEx.Json.JsonSerializer.Default is Text.Json.JsonSerializer js ? js : new Text.Json.JsonSerializer());
            if (autoMap)
                AutomagicallyMap(ignoreSrceProperties);
        }

        /// <summary>
        /// Gets the <see cref="Text.Json.JsonSerializer"/>.
        /// </summary>
        public Text.Json.JsonSerializer JsonSerializer { get; }

        /// <inheritdoc/>
        IEnumerable<IPropertyJsonMapper> IJsonObjectMapperMappings.Mappings => _mappings.AsEnumerable();

        /// <inheritdoc/>
        public IPropertyJsonMapper this[string propertyName] => TryGetProperty(propertyName, out var pcm) ? pcm : throw new ArgumentException($"Property '{propertyName}' does not exist.", nameof(propertyName));

        /// <summary>
        /// Gets the <see cref="IPropertyJsonMapper"/> for the specified source <paramref name="propertyExpression"/>.
        /// </summary>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <returns>The <see cref="IPropertyJsonMapper"/> where found.</returns>
        /// <exception cref="ArgumentException">Thrown when the property does not exist.</exception>
        public IPropertyJsonMapper this[Expression<Func<TSource, object?>> propertyExpression]
        {
            get
            {
                MemberExpression? me = null;
                if (propertyExpression.ThrowIfNull(nameof(propertyExpression)).Body.NodeType == ExpressionType.MemberAccess)
                    me = propertyExpression.Body as MemberExpression;
                else if (propertyExpression.Body.NodeType == ExpressionType.Convert)
                {
                    if (propertyExpression.Body is UnaryExpression ue)
                        me = ue.Operand as MemberExpression;
                }

                if (me == null)
                    throw new InvalidOperationException("Only Member access expressions are supported.");

                return this[me.Member.Name];
            }
        }

        /// <inheritdoc/>
        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IPropertyJsonMapper? propertyColumnMapper)
        {
            propertyColumnMapper = _mappings.Where(x => x.PropertyName == propertyName).FirstOrDefault();
            return propertyColumnMapper != null;
        }

        /// <inheritdoc/>
        public string GetJsonName(string propertyName) => this[propertyName].JsonName;

        /// <summary>
        /// Automatically add each public get/set property.
        /// </summary>
        private void AutomagicallyMap(string[] ignoreSrceProperties)
        {
            foreach (var sp in TypeReflector.GetProperties(typeof(TSource)))
            {
                // Do not auto-map where ignore has been specified.
                if (ignoreSrceProperties.Contains(sp.Name))
                    continue;

                // Create the lambda expression for the property and add to the mapper.
                var spe = Expression.Parameter(typeof(TSource), "x");
                var sex = Expression.Lambda(Expression.Property(spe, sp), spe);
                typeof(JsonObjectMapper<TSource>)
                    .GetMethod(nameof(AutoProperty), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod([sp.PropertyType])
                    .Invoke(this, [sex, null, OperationTypes.Any]);
            }
        }

        /// <summary>
        /// Adds a <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> to the mapper with additiional auto-logic.
        /// </summary>
        private PropertyJsonMapper<TSource, TSourceProperty> AutoProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? jsonName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            var pcm = Property(propertyExpression, jsonName, operationTypes);

            // Automatically set primary key where IIdentifier.
            if (_implementsIIdentifier && pcm.PropertyName == nameof(IIdentifier.Id))
                pcm.SetPrimaryKey();

            return pcm;
        }

        /// <summary>
        /// Adds a <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>Dataverse</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <returns>The <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/>.</returns>
        public PropertyJsonMapper<TSource, TSourceProperty> Property<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            var pcm = new PropertyJsonMapper<TSource, TSourceProperty>(this, propertyExpression, columnName, operationTypes);
            AddMapping(pcm);
            return pcm;
        }

        /// <summary>
        /// Validates and adds a new IPropertyJsonMapper.
        /// </summary>
        private void AddMapping<TSourceProperty>(PropertyJsonMapper<TSource, TSourceProperty> propertyJsonMapper)
        {
            if (_mappings.Any(x => x.PropertyName == propertyJsonMapper.PropertyName))
                throw new ArgumentException($"Source property '{propertyJsonMapper.PropertyName}' must not be specified more than once.", nameof(propertyJsonMapper));

            if (_mappings.Any(x => x.JsonName == propertyJsonMapper.JsonName))
                throw new ArgumentException($"Column '{propertyJsonMapper.JsonName}' must not be specified more than once.", nameof(propertyJsonMapper));

            _mappings.Add(propertyJsonMapper);
        }

        /// <summary>
        /// Adds or updates <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="jsonName">The JSON property name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <param name="property">An <see cref="Action"/> enabling access to the created <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/>.</param>
        /// <returns></returns>
        /// <remarks>Where updating an existing the <paramref name="jsonName"/> and <paramref name="operationTypes"/> where specified will override the previous values.</remarks>
        public JsonObjectMapper<TSource> HasProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? jsonName = null, OperationTypes? operationTypes = null, Action<PropertyJsonMapper<TSource, TSourceProperty>>? property = null)
        {
            var tmp = new PropertyJsonMapper<TSource, TSourceProperty>(this, propertyExpression, jsonName, operationTypes ?? OperationTypes.Any);
            var pcm = _mappings.Where(x => x.PropertyName == tmp.PropertyName).OfType<PropertyJsonMapper<TSource, TSourceProperty>>().SingleOrDefault();
            if (pcm == null)
                AddMapping(pcm = tmp);
            else
            {
                if (jsonName != null && tmp.JsonName != pcm.JsonName)
                {
                    if (_mappings.Any(x => x.JsonName == pcm.JsonName))
                        throw new ArgumentException($"JSON property '{pcm.JsonName}' must not be specified more than once.", nameof(jsonName));
                    else
                        pcm.JsonName = tmp.JsonName;
                }

                if (operationTypes != null)
                    pcm.OperationTypes = operationTypes.Value;
            }

            property?.Invoke(pcm);
            return this;
        }

        /// <summary>
        /// Inherits the property mappings from the selected <paramref name="inheritMapper"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="inheritMapper"/> source <see cref="Type"/>. Must inherit from <typeparamref name="TSource"/>.</typeparam>
        /// <param name="inheritMapper">The <see cref="IJsonObjectMapper{T}"/> to inherit from. Must also implement <see cref="IJsonObjectMapperMappings"/>.</param>
        public void InheritPropertiesFrom<T>(IJsonObjectMapper<T> inheritMapper) where T : class, new()
        {
            inheritMapper.ThrowIfNull(nameof(inheritMapper));
            if (!typeof(TSource).IsSubclassOf(typeof(T))) throw new ArgumentException($"Type {typeof(TSource).Name} must inherit from {typeof(T).Name}.", nameof(inheritMapper));
            if (inheritMapper is not IJsonObjectMapperMappings inheritMappings) throw new ArgumentException($"Type {typeof(T).Name} must implement {typeof(IJsonObjectMapperMappings).Name} to copy the mappings.", nameof(inheritMapper));

            var pe = Expression.Parameter(typeof(TSource), "x");
            var type = typeof(JsonObjectMapper<>).MakeGenericType(typeof(TSource));

            foreach (var p in inheritMappings.Mappings)
            {
                var lex = Expression.Lambda(Expression.Property(pe, p.PropertyName), pe);
                var pmap = (IPropertyJsonMapper)type
                    .GetMethod("Property", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(this, [lex, p.JsonName, p.OperationTypes])!;

                if (p.IsPrimaryKey)
                    pmap.SetPrimaryKey();

                if (p.Converter != null)
                    pmap.SetConverter(p.Converter);

                if (p.Mapper != null)
                    pmap.SetMapper(p.Mapper);
            }
        }

        /// <inheritdoc/>
        public void MapToJson(TSource? value, JsonObject json, OperationTypes operationType = OperationTypes.Unspecified)
        {
            json.ThrowIfNull(nameof(json));
            if (value == null) return;

            foreach (var p in _mappings)
            {
                p.MapToJson(value, json, operationType);
            }

            OnMapToJson(value, json, operationType);
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapToJson(TSource, JsonObject, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="json">The <see cref="JsonObject"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        protected virtual void OnMapToJson(TSource value, JsonObject json, OperationTypes operationType) { }

        /// <inheritdoc/>
        public TSource? MapFromJson(JsonObject json, OperationTypes operationType = OperationTypes.Unspecified)
        {
            json.ThrowIfNull(nameof(json));
            var value = new TSource();

            foreach (var p in _mappings)
            {
                p.MapFromJson(json, value, operationType);
            }

            value = OnMapFromJson(value, json, operationType);
            return (value != null && value is IInitial ii && ii.IsInitial) ? null : value;
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapFromJson(JsonObject, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="json">The <see cref="JsonObject"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The source value.</returns>
        protected virtual TSource? OnMapFromJson(TSource value, JsonObject json, OperationTypes operationType) => value;
    }
}