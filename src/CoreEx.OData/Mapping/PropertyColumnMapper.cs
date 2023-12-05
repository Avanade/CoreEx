// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping;
using CoreEx.Mapping.Converters;
using System;
using System.Linq.Expressions;

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Provides bi-directional property and <see cref="ODataItem"/> column mapping.
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
        /// <param name="columnName">The <i>Dictionary</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
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
        public IConverter? Converter { get; private set; }

        /// <inheritdoc/>
        public IODataMapper? Mapper { get; private set; }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetConverter(IConverter converter)
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
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyColumnMapper<TSource, TSourceProperty> SetConverter<TDestinationProperty>(IConverter<TSourceProperty, TDestinationProperty> converter)
        {
            ((IPropertyColumnMapper)this).SetConverter(converter);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetMapper(IODataMapper mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            if (Converter != null)
                throw new InvalidOperationException("The Mapper and Converter cannot be both set; only one is permissible.");

            if (!_propertyExpression.IsClass)
                throw new InvalidOperationException($"The PropertyType '{PropertyType.Name}' must be a class to set a Mapper.");

            if (mapper.SourceType != typeof(TSourceProperty))
                throw new ArgumentNullException($"The PropertyType '{PropertyType.Name}' and IDictionaryMapper.SourceType '{mapper.SourceType.Name}' must match.");

            Mapper = mapper;
        }

        /// <summary>
        /// Sets the <see cref="Mapper"/>.
        /// </summary>
        /// <param name="mapper">The <see cref="IODataMapper"/>.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyColumnMapper<TSource, TSourceProperty> SetMapper<TDestinationProperty>(IODataMapper<TDestinationProperty> mapper)
        {
            ((IPropertyColumnMapper)this).SetMapper(mapper);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.MapToOData(object? value, ODataItem entity, OperationTypes operationType) => MapToDictionary((TSource?)value, entity, operationType);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="entity"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="ODataItem"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapToDictionary(TSource? value, ODataItem entity, OperationTypes operationType)
        {
            if (value == null || !OperationTypes.HasFlag(operationType))
                return;

            var val = _propertyExpression.GetValue(value);
            if (Mapper != null)
            {
                if (val != null)
                    Mapper.MapToOData(val, entity, operationType);
            }
            else
            {
                var aval = Converter == null ? val : Converter.ConvertToDestination(val);
                entity[ColumnName] = aval!;
            }
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.MapFromOData(ODataItem entity, object value, OperationTypes operationType) => MapFromDictionary(entity, (TSource)value, operationType);

        /// <summary>
        /// Maps from a <paramref name="entity"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="entity">The <see cref="ODataItem"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapFromDictionary(ODataItem entity, TSource value, OperationTypes operationType)
        {
            if (!OperationTypes.HasFlag(operationType))
                return;

            TSourceProperty? pval;
            if (Mapper != null)
                pval = (TSourceProperty?)Mapper.MapFromOData(entity, operationType);
            else
            {
                if (Converter is null)
                    pval = (TSourceProperty)entity[ColumnName]!;
                else
                    pval = (TSourceProperty)Converter.ConvertToSource(entity[ColumnName])!;
            }

            _propertyExpression.SetValue(value, pval);
        }
    }
}