// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Mapping;
using CoreEx.Mapping.Converters;
using System;
using System.Data;
using System.Linq.Expressions;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Provides bi-directional property and database column mapping.
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
        /// <param name="columnName">The database column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="parameterName">The database parameter name. Defaults to <paramref name="columnName"/> prefixed with '<c>@</c>'.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        public PropertyColumnMapper(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, string? parameterName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            _propertyExpression = Abstractions.Reflection.PropertyExpression.Create(propertyExpression);
            ColumnName = columnName ?? PropertyName;
            ParameterName = parameterName ?? $"@{columnName ?? PropertyName}";
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
        public string ParameterName { get; internal set; }

        /// <inheritdoc/>
        public OperationTypes OperationTypes { get; internal set; }

        /// <inheritdoc/>
        public bool IsPrimaryKey { get; private set; }

        /// <inheritdoc/>
        public bool IsPrimaryKeyGeneratedOnCreate { get; private set; }

        /// <inheritdoc/>
        public DbType? DbType { get; private set; }

        /// <inheritdoc/>
        public IConverter? Converter { get; private set; }

        /// <inheritdoc/>
        public IDatabaseMapper? Mapper { get; private set; }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetDbType(DbType dbType) => SetDbType(dbType);

        /// <summary>
        /// Sets the <see cref="DbType"/>.
        /// </summary>
        /// <param name="dbType">The <see cref="System.Data.DbType"/></param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> SetDbType(DbType dbType)
        {
            DbType = dbType;
            return this;
        }

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
        void IPropertyColumnMapper.SetMapper(IDatabaseMapper mapper)
        {
            mapper.ThrowIfNull(nameof(mapper));

            if (Converter != null)
                throw new InvalidOperationException("The Mapper and Converter cannot be both set; only one is permissible.");

            if (!_propertyExpression.IsClass)
                throw new InvalidOperationException($"The PropertyType '{PropertyType.Name}' must be a class to set a Mapper.");

            if (mapper.SourceType != typeof(TSourceProperty))
                throw new ArgumentException($"The PropertyType '{PropertyType.Name}' and IDatabaseMapper.SourceType '{mapper.SourceType.Name}' must match.", nameof(mapper));

            if (IsPrimaryKey)
                throw new InvalidOperationException("A Mapper can not be set for a primary key.");

            Mapper = mapper;
        }

        /// <summary>
        /// Sets the <see cref="Mapper"/>.
        /// </summary>
        /// <param name="mapper">The <see cref="IDatabaseMapper"/>.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The <see cref="Converter"/> and <see cref="Mapper"/> are mutually exclusive.</remarks>
        public PropertyColumnMapper<TSource, TSourceProperty> SetMapper<TDestinationProperty>(IDatabaseMapper<TDestinationProperty> mapper)
        {
            ((IPropertyColumnMapper)this).SetMapper(mapper);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.SetPrimaryKey(bool generatedOnCreate)
        {
            if (Mapper != null) throw new InvalidOperationException("A primary key must not contain a Mapper.");

            IsPrimaryKey = true;
            IsPrimaryKeyGeneratedOnCreate = generatedOnCreate;
        }

        /// <summary>
        /// Sets the primary key (<see cref="IsPrimaryKey"/> and <see cref="IsPrimaryKeyGeneratedOnCreate"/>).
        /// </summary>
        /// <param name="generatedOnCreate">Indicates whether the column value is generated on create. Defaults to <c>true</c>.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to support fluent-style method-chaining.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> SetPrimaryKey(bool generatedOnCreate = true)
        {
            ((IPropertyColumnMapper)this).SetPrimaryKey(generatedOnCreate);
            return this;
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.MapToDb(object? value, DatabaseParameterCollection parameters, OperationTypes operationType) => MapToDb((TSource?)value, parameters, operationType);

        /// <summary>
        /// Maps from a <paramref name="value"/> updating the <paramref name="parameters"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapToDb(TSource? value, DatabaseParameterCollection parameters, OperationTypes operationType)
        {
            if (value == null || !OperationTypes.HasFlag(operationType))
                return;

            if (parameters.Contains(ParameterName))
                return;

            var val = _propertyExpression.GetValue(value);
            if (Mapper != null)
            {
                if (val != null)
                    Mapper.MapToDb(val, parameters, operationType);
            }
            else
            {
                if (DbType.HasValue)
                    parameters.AddParameter(ParameterName, Converter == null ? val : Converter.ConvertToDestination(val), DbType.Value);
                else
                    parameters.AddParameter(ParameterName, Converter == null ? val : Converter.ConvertToDestination(val));
            }
        }

        /// <inheritdoc/>
        void IPropertyColumnMapper.MapFromDb(DatabaseRecord record, object value, OperationTypes operationType) => MapFromDb(record, (TSource)value, operationType);

        /// <summary>
        /// Maps from a <paramref name="record"/> updating the <paramref name="value"/>.
        /// </summary>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        private void MapFromDb(DatabaseRecord record, TSource value, OperationTypes operationType)
        {
            if (!OperationTypes.HasFlag(operationType))
                return;

            TSourceProperty? pval = default;
            if (Mapper != null)
                pval = (TSourceProperty?)Mapper.MapFromDb(record, operationType);
            else
            {
                if (!record.IsDBNull(ColumnName, out var ordinal))
                {
                    if (Converter == null)
                        pval = record.GetValue<TSourceProperty>(ordinal);
                    else
                        pval = (TSourceProperty)Converter.ConvertToSource(record.DataReader.GetValue(ordinal))!;
                }
            }

            _propertyExpression.SetValue(value, pval);
        }
    }
}