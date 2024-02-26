// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping;
using CoreEx.Mapping.Converters;
using Microsoft.Xrm.Sdk;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CoreEx.Dataverse.Mapping
{
    /// <summary>
    /// Provides bi-directional property and <i>Dataverse</i> column mapping.
    /// </summary>
    /// <typeparam name="TSource">The source entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSourceProperty">The corresponding source property <see cref="Type"/>.</typeparam>
    public class PropertyColumnMapper<TSource, TSourceProperty> : IPropertyColumnMapper where TSource : class, new()
    {
        private readonly PropertyExpression<TSource, TSourceProperty> _propertyExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> class.
        /// </summary>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>Dataverse</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        internal PropertyColumnMapper(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            _propertyExpression = Abstractions.Reflection.PropertyExpression.Create(propertyExpression);
            ColumnName = columnName ?? PropertyName;
            OperationTypes = operationTypes;
        }

        /// <inheritdoc/>
        public IPropertyExpression PropertyExpression => _propertyExpression;

        /// <inheritdoc/>
        public string PropertyName => _propertyExpression.Name;

        /// <inheritdoc/>
        public Type PropertyType => typeof(TSourceProperty);

        /// <inheritdoc/>
        public bool IsSrcePropertyComplex => throw new NotImplementedException();

        /// <inheritdoc/>
        public string ColumnName { get; internal set; }

        /// <inheritdoc/>
        public OperationTypes OperationTypes { get; internal set; }

        /// <inheritdoc/>
        public bool IsPrimaryKey { get; private set; }

        /// <inheritdoc/>
        public bool IsPrimaryKeyUseEntityIdentifier { get; private set; }

        /// <inheritdoc/>
        public IConverter? Converter { get; private set; }

        /// <inheritdoc/>
        public IDataverseMapper? Mapper { get; private set; }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetConverter(IConverter converter)
        {
            converter.ThrowIfNull(nameof(converter));

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
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyColumnMapper<TSource, TSourceProperty> SetConverter<TDestinationProperty>(IConverter<TSourceProperty, TDestinationProperty> converter)
        {
            ((IPropertyColumnMapper)this).SetConverter(converter);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetMapper(IDataverseMapper mapper)
        {
            mapper.ThrowIfNull(nameof(mapper));

            if (Converter != null)
                throw new InvalidOperationException("The Mapper and Converter cannot be both set; only one is permissible.");

            if (!_propertyExpression.IsClass)
                throw new InvalidOperationException($"The PropertyType '{PropertyType.Name}' must be a class to set a Mapper.");

            if (mapper.SourceType != typeof(TSourceProperty))
                throw new ArgumentException($"The PropertyType '{PropertyType.Name}' and IDataverseMapper.SourceType '{mapper.SourceType.Name}' must match.", nameof(mapper));

            if (IsPrimaryKey)
                throw new InvalidOperationException("A Mapper can not be set for a primary key.");

            Mapper = mapper;
        }

        /// <summary>
        /// Sets the <see cref="Mapper"/>.
        /// </summary>
        /// <param name="mapper">The <see cref="IDataverseMapper"/>.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyColumnMapper<TSource, TSourceProperty> SetMapper<TDestinationProperty>(IDataverseMapper<TDestinationProperty> mapper)
        {
            ((IPropertyColumnMapper)this).SetMapper(mapper);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetPrimaryKey(bool useEntityIdentifier)
        {
            if (Mapper != null) throw new InvalidOperationException("A primary key must not contain a Mapper.");

            if (useEntityIdentifier)
            {
                if (PropertyType == typeof(Guid) || PropertyType == typeof(Guid?) || PropertyType == typeof(string))
                    IsPrimaryKeyUseEntityIdentifier = true;
                else
                    throw new InvalidOperationException($"The PropertyType '{PropertyType.Name}' must be a Guid or String to use the Entity.Id.");
            }

            IsPrimaryKey = true;
        }

        /// <summary>
        /// Sets the primary key (<see cref="IsPrimaryKey"/>).
        /// </summary>
        /// <param name="useEntityIdentifier">Indicates whether the primary key value maps to the underlying <see cref="Entity.Id"/> versus <see cref="Entity.KeyAttributes"/>.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> SetPrimaryKey(bool useEntityIdentifier = true)
        {
            ((IPropertyColumnMapper)this).SetPrimaryKey(useEntityIdentifier);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.MapToDataverse(object? value, Entity entity, OperationTypes operationType) => MapToDataverse((TSource?)value, entity, operationType);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="Entity"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapToDataverse(TSource? value, Entity entity, OperationTypes operationType)
        {
            if (value == null || !OperationTypes.HasFlag(operationType))
                return;

            var val = _propertyExpression.GetValue(value);
            if (Mapper != null)
            {
                if (val != null)
                    Mapper.MapToDataverse(val, entity, operationType);
            }
            else
            {
                if (IsPrimaryKey && IsPrimaryKeyUseEntityIdentifier)
                {
                    entity.Id = PropertyType == typeof(string) ? Guid.Parse(val?.ToString() ?? string.Empty) : (Guid)Convert.ChangeType(val, typeof(Guid))!;
                    return;
                }

                var aval = Converter == null ? val : Converter.ConvertToDestination(val);
                if (IsPrimaryKey)
                    entity.KeyAttributes.Add(ColumnName, aval);
                else
                    entity.Attributes.Add(ColumnName, aval);
            }
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.MapFromDataverse(Entity entity, object value, OperationTypes operationType) => MapFromDataverse(entity, (TSource)value, operationType);

        /// <summary>
        /// Maps from a <paramref name="entity"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapFromDataverse(Entity entity, TSource value, OperationTypes operationType)
        {
            if (!OperationTypes.HasFlag(operationType))
                return;

            TSourceProperty? pval;
            if (Mapper != null)
                pval = (TSourceProperty?)Mapper.MapFromDataverse(entity, operationType);
            else
            {
                if (IsPrimaryKey)
                {
                    if (IsPrimaryKeyUseEntityIdentifier)
                        pval = (TSourceProperty?)TypeDescriptor.GetConverter(PropertyType).ConvertFromInvariantString(entity.Id.ToString());
                    else
                    {
                        if (Converter is null)
                            pval = (TSourceProperty)entity.KeyAttributes[ColumnName];
                        else
                            pval = (TSourceProperty)Converter.ConvertToSource(entity.KeyAttributes[ColumnName])!;
                    }
                }
                else
                {
                    if (Converter is null)
                        pval = entity.GetAttributeValue<TSourceProperty>(ColumnName);
                    else
                        pval = (TSourceProperty)Converter.ConvertToSource(entity.Attributes[ColumnName])!;
                }
            }

            _propertyExpression.SetValue(value, pval);
        }
    }
}