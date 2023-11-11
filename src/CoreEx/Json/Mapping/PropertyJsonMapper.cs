// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping;
using CoreEx.Mapping.Converters;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Json.Mapping
{
    /// <summary>
    /// Provides bi-directional property and <see cref="JsonObject"/> property mapping.
    /// </summary>
    /// <typeparam name="TSource">The source entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSourceProperty">The corresponding source property <see cref="Type"/>.</typeparam>
    public class PropertyJsonMapper<TSource, TSourceProperty> : IPropertyJsonMapper where TSource : class, new()
    {
        private readonly PropertyExpression<TSource, TSourceProperty> _propertyExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="IJsonObjectMapper"/>.</param>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="jsonName">The <see cref="JsonObject"/> property name. Defaults to the JSON name inferred by the <paramref name="propertyExpression"/>.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        internal PropertyJsonMapper(IJsonObjectMapper owner, Expression<Func<TSource, TSourceProperty>> propertyExpression, string? jsonName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _propertyExpression = Abstractions.Reflection.PropertyExpression.Create(propertyExpression, Owner.JsonSerializer);
            JsonName = jsonName ?? _propertyExpression.JsonName ?? PropertyName;
            OperationTypes = operationTypes;
        }

        /// <summary>
        /// Gets the owning <see cref="IJsonObjectMapper"/>.
        /// </summary>
        /// <remarks>Required to provide the underlying <see cref="IJsonObjectMapper.JsonSerializer"/>.</remarks>
        public IJsonObjectMapper Owner { get; }

        /// <inheritdoc/>
        public IPropertyExpression PropertyExpression => _propertyExpression;

        /// <inheritdoc/>
        public string PropertyName => _propertyExpression.Name;

        /// <inheritdoc/>
        public Type PropertyType => typeof(TSourceProperty);

        /// <inheritdoc/>
        public bool IsSrcePropertyComplex => throw new NotImplementedException();

        /// <inheritdoc/>
        public string JsonName { get; internal set; }

        /// <inheritdoc/>
        public OperationTypes OperationTypes { get; internal set; }

        /// <inheritdoc/>
        public bool IsPrimaryKey { get; private set; }

        /// <inheritdoc/>
        public bool IsPrimaryKeyUseEntityIdentifier { get; private set; }

        /// <inheritdoc/>
        public IConverter? Converter { get; private set; }

        /// <inheritdoc/>
        public IJsonObjectMapper? Mapper { get; private set; }

        /// <inheritdoc/>
        void IPropertyJsonMapper.SetConverter(IConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            if (Mapper != null)
                throw new InvalidOperationException("The Mapper and Converter cannot be both set; only one is permissible.");

            if (converter.SourceType != typeof(TSourceProperty))
                throw new InvalidOperationException($"The PropertyType '{PropertyType.Name}' and IConverter.SourceType '{converter.SourceType.Name}' must match.");

            Converter = converter;
        }

        /// <summary>
        /// Sets the <see cref="Converter"/>.
        /// </summary>
        /// <param name="converter">The <see cref="IConverter"/>.</param>
        /// <returns>The <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyJsonMapper<TSource, TSourceProperty> SetConverter<TDestinationProperty>(IConverter<TSourceProperty, TDestinationProperty> converter)
        {
            ((IPropertyJsonMapper)this).SetConverter(converter);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyJsonMapper.SetMapper(IJsonObjectMapper mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            if (Converter != null)
                throw new InvalidOperationException("The Mapper and Converter cannot be both set; only one is permissible.");

            if (!_propertyExpression.IsClass)
                throw new InvalidOperationException($"The PropertyType '{PropertyType.Name}' must be a class to set a Mapper.");

            if (mapper.SourceType != typeof(TSourceProperty))
                throw new ArgumentNullException($"The PropertyType '{PropertyType.Name}' and IDataverseMapper.SourceType '{mapper.SourceType.Name}' must match.");

            if (IsPrimaryKey)
                throw new InvalidOperationException("A Mapper can not be set for a primary key.");

            Mapper = mapper;
        }

        /// <summary>
        /// Sets the <see cref="Mapper"/>.
        /// </summary>
        /// <param name="mapper">The <see cref="IJsonObjectMapper"/>.</param>
        /// <returns>The <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyJsonMapper<TSource, TSourceProperty> SetMapper<TDestinationProperty>(IJsonObjectMapper<TDestinationProperty> mapper)
        {
            ((IPropertyJsonMapper)this).SetMapper(mapper);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyJsonMapper.SetPrimaryKey()
        {
            if (Mapper != null) throw new InvalidOperationException("A primary key must not contain a Mapper.");

            IsPrimaryKey = true;
        }

        /// <summary>
        /// Sets the primary key (<see cref="IsPrimaryKey"/>).
        /// </summary>
        /// <returns>The <see cref="PropertyJsonMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        public PropertyJsonMapper<TSource, TSourceProperty> SetPrimaryKey()
        {
            ((IPropertyJsonMapper)this).SetPrimaryKey();
            return this;
        }

        /// <inheritdoc/>
        void IPropertyJsonMapper.MapToJson(object? value, JsonObject json, OperationTypes operationType) => MapToJson((TSource?)value, json, operationType);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="json"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="json">The <see cref="JsonObject"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapToJson(TSource? value, JsonObject json, OperationTypes operationType)
        {
            if (value == null || !OperationTypes.HasFlag(operationType))
                return;

            var val = _propertyExpression.GetValue(value);
            if (Mapper != null)
            {
                if (val != null)
                    Mapper.MapToJson(val, json, operationType);
            }
            else
            {
                var jv = Converter == null ? val : Converter.ConvertToDestination(val);
                json[JsonName] = System.Text.Json.JsonSerializer.SerializeToNode(jv, Owner.JsonSerializer.Options);
            }
        }

        /// <inheritdoc/>
        void IPropertyJsonMapper.MapFromJson(JsonObject json, object value, OperationTypes operationType) => MapFromJson(json, (TSource)value, operationType);

        /// <summary>
        /// Maps from a <paramref name="json"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonObject"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapFromJson(JsonObject json, TSource value, OperationTypes operationType)
        {
            if (!OperationTypes.HasFlag(operationType))
                return;

            TSourceProperty? pval;
            if (Mapper != null)
                pval = (TSourceProperty?)Mapper.MapFromJson(json, operationType);
            else
            {
                // Where no json property found then set to default.
                if (json.TryGetPropertyValue(JsonName, out var jn) || jn == null)
                {
                    if (Converter is null)
                        pval = (TSourceProperty)jn.Deserialize(PropertyType, Owner.JsonSerializer.Options)!;
                    else
                        pval = (TSourceProperty)Converter.ConvertToSource(jn.Deserialize(Converter.DestinationType, Owner.JsonSerializer.Options))!;
                }
                else
                    pval = default;
            }

            _propertyExpression.SetValue(value, pval);
        }
    }
}