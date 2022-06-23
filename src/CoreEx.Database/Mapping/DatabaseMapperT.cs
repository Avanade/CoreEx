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

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Provides mapping from a <typeparamref name="TSource"/> <see cref="Type"/> and database.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    public class DatabaseMapper<TSource> : IDatabaseMapper<TSource>, IDatabaseMapperMappings where TSource : class, new()
    {
        private readonly List<IPropertyColumnMapper> _mappings = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMapper{TSource}"/> class.
        /// </summary>
        /// <param name="autoMap">Indicates whether the entity should automatically map all public get/set properties, where the property, column and parameter names are all assumed to share the same name.</param>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        public DatabaseMapper(bool autoMap = false, params string[] ignoreSrceProperties)
        {
            if (typeof(TSource) == typeof(string)) throw new InvalidOperationException("TSource must not be a String.");

            if (autoMap)
                AutomagicallyMap(ignoreSrceProperties);
        }

        /// <inheritdoc/>
        IEnumerable<IPropertyColumnMapper> IDatabaseMapperMappings.Mappings => _mappings.AsEnumerable();

        /// <inheritdoc/>
        public IPropertyColumnMapper this[string propertyName] => TryGetProperty(propertyName, out var pcm) ? pcm : throw new ArgumentException($"Property '{propertyName}' does not exist.", nameof(propertyName));

        /// <summary>
        /// Gets the <see cref="IPropertyColumnMapper"/> for the specified source <paramref name="propertyExpression"/>.
        /// </summary>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <returns>The <see cref="IPropertyColumnMapper"/> where found.</returns>
        /// <exception cref="ArgumentException">Thrown when the property does not exist.</exception>
        public IPropertyColumnMapper this[Expression<Func<TSource, object?>> propertyExpression]
        {
            get
            {
                if (propertyExpression == null)
                    throw new ArgumentNullException(nameof(propertyExpression));

                MemberExpression? me = null;
                if (propertyExpression.Body.NodeType == ExpressionType.MemberAccess)
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
        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IPropertyColumnMapper? propertyColumnMapper)
        {
            propertyColumnMapper = _mappings.Where(x => x.PropertyName == propertyName).FirstOrDefault();
            return propertyColumnMapper != null;
        }

        /// <inheritdoc/>
        public string GetParameterName(string propertyName) => this[propertyName].ParameterName;

        /// <inheritdoc/>
        public string GetColumnName(string propertyName) => this[propertyName].ColumnName;

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
                typeof(DatabaseMapper<TSource>)
                    .GetMethod("Property", BindingFlags.Public | BindingFlags.Instance)
                    .MakeGenericMethod(new Type[] { sp.PropertyType })
                    .Invoke(this, new object?[] { sex, null, null, OperationTypes.Any });
            }
        }

        /// <summary>
        /// Adds a <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The database column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="parameterName">The database parameter name. Defaults to <paramref name="columnName"/> prefixed with '<c>@</c>'.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> Property<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, string? parameterName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            var pcm = new PropertyColumnMapper<TSource, TSourceProperty>(propertyExpression, columnName, parameterName, operationTypes);
            AddMapping(pcm);
            return pcm;
        }

        /// <summary>
        /// Validates and adds a new IPropertyColumnMapper.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "They are the arguments from the calling method.")]
        private void AddMapping(IPropertyColumnMapper pcm)
        {
            if (_mappings.Any(x => x.PropertyName == pcm.PropertyName))
                throw new ArgumentException($"Source property '{pcm.PropertyName}' must not be specified more than once.", "propertyExpression");

            if (_mappings.Any(x => x.ColumnName == pcm.ColumnName))
                throw new ArgumentException($"Column '{pcm.ColumnName}' must not be specified more than once.", "columnName");

            if (_mappings.Any(x => x.ParameterName == pcm.ParameterName))
                throw new ArgumentException($"Parameter '{pcm.ParameterName}' must not be specified more than once.", "parameterName");

            _mappings.Add(pcm);
        }

        /// <summary>
        /// Adds or updates <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The database column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="parameterName">The database parameter name. Defaults to <paramref name="columnName"/> prefixed with '<c>@</c>'.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <param name="property">An <see cref="Action"/> enabling access to the created <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</param>
        /// <returns></returns>
        /// <remarks>Where updating an existing the <paramref name="columnName"/>, <paramref name="parameterName"/> and <paramref name="operationTypes"/> where specified will override the previous values.</remarks>
        public DatabaseMapper<TSource> HasProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, string? parameterName = null, OperationTypes? operationTypes = null, Action<PropertyColumnMapper<TSource, TSourceProperty>>? property = null)
        {
            var tmp = new PropertyColumnMapper<TSource, TSourceProperty>(propertyExpression, columnName, parameterName, operationTypes ?? OperationTypes.Any);
            var pcm = _mappings.Where(x => x.PropertyName == tmp.PropertyName).OfType<PropertyColumnMapper<TSource, TSourceProperty>>().SingleOrDefault();
            if (pcm == null)
                AddMapping(pcm = tmp);
            else
            {
                if (columnName != null && tmp.ColumnName != pcm.ColumnName)
                {
                    if (_mappings.Any(x => x.ColumnName == pcm.ColumnName))
                        throw new ArgumentException($"Column '{pcm.ColumnName}' must not be specified more than once.", nameof(columnName));
                    else
                        pcm.ColumnName = tmp.ColumnName;
                }

                if (parameterName != null && tmp.ParameterName != pcm.ParameterName)
                {
                    if (_mappings.Any(x => x.ParameterName == tmp.ParameterName))
                        throw new ArgumentException($"Parameter '{pcm.ParameterName}' must not be specified more than once.", nameof(parameterName));
                    else
                        pcm.ParameterName = tmp.ParameterName;
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
        /// <param name="inheritMapper">The <see cref="IDatabaseMapper{T}"/> to inherit from. Must also implement <see cref="IDatabaseMapperMappings"/>.</param>
        public void InheritPropertiesFrom<T>(IDatabaseMapper<T> inheritMapper) where T : class, new()
        {
            if (inheritMapper == null) throw new ArgumentNullException(nameof(inheritMapper));
            if (!typeof(TSource).IsSubclassOf(typeof(T))) throw new ArgumentException($"Type {typeof(TSource).Name} must inherit from {typeof(T).Name}.", nameof(inheritMapper));
            if (inheritMapper is not IDatabaseMapperMappings inheritMappings) throw new ArgumentException($"Type {typeof(T).Name} must implement {typeof(IDatabaseMapperMappings).Name} to copy the mappings.", nameof(inheritMapper));

            var pe = Expression.Parameter(typeof(TSource), "x");
            var type = typeof(DatabaseMapper<>).MakeGenericType(typeof(TSource));

            foreach (var p in inheritMappings.Mappings)
            {
                var lex = Expression.Lambda(Expression.Property(pe, p.PropertyName), pe);
                var pmap = (IPropertyColumnMapper)type
                    .GetMethod("Property", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(this, new object?[] { lex, p.ColumnName, p.ParameterName, p.OperationTypes });

                if (p.IsPrimaryKey)
                    pmap.SetPrimaryKey(p.IsPrimaryKeyGeneratedOnCreate);

                if (p.DbType != null)
                    pmap.SetDbType(p.DbType.Value);

                if (p.Converter != null)
                    pmap.SetConverter(p.Converter);

                if (p.Mapper != null)
                    pmap.SetMapper(p.Mapper);
            }
        }

        /// <inheritdoc/>
        public void MapToDb(TSource? value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (value == null) return;

            foreach (var p in _mappings)
            {
                p.MapToDb(value, parameters, operationType);
            }

            OnMapToDb(value, parameters, operationType);
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapToDb(TSource, DatabaseParameterCollection, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        protected virtual void OnMapToDb(TSource value, DatabaseParameterCollection parameters, OperationTypes operationType) { }

        /// <inheritdoc/>
        public TSource? MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            var value = new TSource();

            foreach (var p in _mappings)
            {
                p.MapFromDb(record, value, operationType);
            }

            value = OnMapFromDb(value, record, operationType);
            return (value != null && value is IInitial ii && ii.IsInitial) ? null : value;
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapFromDb(DatabaseRecord, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The source value.</returns>
        protected virtual TSource? OnMapFromDb(TSource value, DatabaseRecord record, OperationTypes operationType) => value;

        /// <inheritdoc/>
        void IDatabaseMapper<TSource>.MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, TSource? value)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (value == null) return;

            foreach (var p in _mappings.Where(x => x.IsPrimaryKey))
            {
                var dir = (operationType == OperationTypes.Create && p.IsPrimaryKeyGeneratedOnCreate) ? ParameterDirection.Output : ParameterDirection.Input;
                var pval = DatabaseMapper<TSource>.ConvertPropertyValueForDb(p, p.PropertyExpression.GetValue(value));

                if (p.DbType.HasValue)
                    parameters.AddParameter(p.ParameterName, pval, p.DbType.Value, dir);
                else
                    parameters.AddParameter(p.ParameterName, pval, dir);
            }
        }

        /// <summary>
        /// Converts the property value for the database.
        /// </summary>
        private static object? ConvertPropertyValueForDb(IPropertyColumnMapper pcm, object? value) => pcm.Converter == null ? value : pcm.Converter.ConvertToDestination(value);

        /// <inheritdoc/>
        void IDatabaseMapper.MapPrimaryKeyParameters(DatabaseParameterCollection parameters, OperationTypes operationType, CompositeKey key)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var pk = _mappings.Where(x => x.IsPrimaryKey).ToArray();
            if (key.Args == null || key.Args.Length != pk.Length)
                throw new ArgumentException($"The number of keys supplied must equal the number of properties identified as {nameof(IPropertyColumnMapper.IsPrimaryKey)}.", nameof(key));

            for (int i = 0; i < key.Args.Length; i++)
            {
                var p = pk[i];
                var dir = (operationType == OperationTypes.Create && p.IsPrimaryKeyGeneratedOnCreate) ? ParameterDirection.Output : ParameterDirection.Input;
                var pval = DatabaseMapper<TSource>.ConvertPropertyValueForDb(p, key.Args[i]);

                if (p.DbType.HasValue)
                    parameters.AddParameter(p.ParameterName, pval, p.DbType.Value, dir);
                else
                    parameters.AddParameter(p.ParameterName, pval, dir);
            }
        }
    }
}