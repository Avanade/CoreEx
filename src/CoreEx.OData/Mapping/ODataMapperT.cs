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

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Provides bidirectional mapping from a <typeparamref name="TSource"/> <see cref="Type"/> to an <see cref="ODataItem"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    public class ODataMapper<TSource> : IODataMapper<TSource>, IBidirectionalMapper<TSource, ODataItem> where TSource : class, new()
    {
        private readonly List<IPropertyColumnMapper> _mappings = [];
        private readonly bool _implementsIIdentifier = typeof(IIdentifier).IsAssignableFrom(typeof(TSource));
        private readonly Lazy<SourceToItemMapper> _mapperFromTo;
        private readonly Lazy<ItemToSourceMapper> _mapperToFrom;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataMapper{TSource}"/> class.
        /// </summary>
        /// <param name="autoMap">Indicates whether the entity should automatically map all public get/set properties, where the property and column names are all assumed to share the same name.</param>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        public ODataMapper(bool autoMap = false, params string[] ignoreSrceProperties)
        {
            if (typeof(TSource) == typeof(string)) throw new InvalidOperationException("TSource must not be a String.");

            if (autoMap)
                AutomagicallyMap(ignoreSrceProperties);

            _mapperFromTo = new Lazy<SourceToItemMapper>(() => new SourceToItemMapper(this));
            _mapperToFrom = new Lazy<ItemToSourceMapper>(() => new ItemToSourceMapper(this));
        }

        /// <summary>
        /// Gets the <see cref="IPropertyColumnMapper"/> mappings.
        /// </summary>
        public IEnumerable<IPropertyColumnMapper> Mappings => _mappings.AsEnumerable();

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
                propertyExpression.ThrowIfNull(nameof(propertyExpression));

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
                typeof(ODataMapper<TSource>)
                    .GetMethod(nameof(AutoProperty), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod([sp.PropertyType])
                    .Invoke(this, [sex, null, OperationTypes.Any]);
            }
        }

        /// <summary>
        /// Adds a <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper with additiional auto-logic.
        /// </summary>
        private PropertyColumnMapper<TSource, TSourceProperty> AutoProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
            => Property(propertyExpression, columnName, operationTypes);

        /// <summary>
        /// Adds a <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper (same as <see cref="Map"/>).
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>OData</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> Property<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            var pcm = new PropertyColumnMapper<TSource, TSourceProperty>(propertyExpression, columnName, operationTypes);
            AddMapping(pcm);
            return pcm;
        }

        /// <summary>
        /// Adds a <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper (same as <see cref="Property"/>).
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>OData</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> Map<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
            => Property(propertyExpression, columnName, operationTypes);

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

            _mappings.Add(pcm);
        }

        /// <summary>
        /// Adds or updates <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper (same as <see cref="HasMap"/>)
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>OData</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <param name="property">An <see cref="Action"/> enabling access to the created <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</param>
        /// <returns></returns>
        /// <remarks>Where updating an existing the <paramref name="columnName"/> and <paramref name="operationTypes"/> where specified will override the previous values.</remarks>
        public ODataMapper<TSource> HasProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes? operationTypes = null, Action<PropertyColumnMapper<TSource, TSourceProperty>>? property = null)
        {
            var tmp = new PropertyColumnMapper<TSource, TSourceProperty>(propertyExpression, columnName, operationTypes ?? OperationTypes.Any);
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

                if (operationTypes != null)
                    pcm.OperationTypes = operationTypes.Value;
            }

            property?.Invoke(pcm);
            return this;
        }

        /// <summary>
        /// Adds or updates <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper (same as <see cref="HasProperty"/>).
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>OData</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <param name="property">An <see cref="Action"/> enabling access to the created <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</param>
        /// <returns></returns>
        /// <remarks>Where updating an existing the <paramref name="columnName"/> and <paramref name="operationTypes"/> where specified will override the previous values.</remarks>
        public ODataMapper<TSource> HasMap<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes? operationTypes = null, Action<PropertyColumnMapper<TSource, TSourceProperty>>? property = null)
            => HasProperty(propertyExpression, columnName, operationTypes, property);

        /// <summary>
        /// Inherits the property mappings from the selected <paramref name="inheritMapper"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="inheritMapper"/> source <see cref="Type"/>. Must inherit from <typeparamref name="TSource"/>.</typeparam>
        /// <param name="inheritMapper">The <see cref="IODataMapper{T}"/> to inherit from.</param>
        public void InheritPropertiesFrom<T>(IODataMapper<T> inheritMapper) where T : class, new()
        {
            inheritMapper.ThrowIfNull(nameof(inheritMapper));
            if (!typeof(TSource).IsSubclassOf(typeof(T))) throw new ArgumentException($"Type {typeof(TSource).Name} must inherit from {typeof(T).Name}.", nameof(inheritMapper));

            var pe = Expression.Parameter(typeof(TSource), "x");
            var type = typeof(ODataMapper<>).MakeGenericType(typeof(TSource));

            foreach (var p in inheritMapper.Mappings)
            {
                var lex = Expression.Lambda(Expression.Property(pe, p.PropertyName), pe);
                var pmap = (IPropertyColumnMapper)type
                    .GetMethod("Property", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(this, [lex, p.ColumnName, p.OperationTypes])!;

                if (p.IsPrimaryKey)
                    pmap.SetPrimaryKey();

                if (p.Converter != null)
                    pmap.SetConverter(p.Converter);

                if (p.Mapper != null)
                    pmap.SetMapper(p.Mapper);
            }
        }

        /// <inheritdoc/>
        public void MapToOData(TSource? value, ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified)
        {
            entity.ThrowIfNull(nameof(entity));
            if (value == null) return;

            foreach (var p in _mappings)
            {
                p.MapToOData(value, entity, operationType);
            }

            OnMapToOData(value, entity, operationType);
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapToOData(TSource, ODataItem, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="ODataItem"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        protected virtual void OnMapToOData(TSource value, ODataItem entity, OperationTypes operationType) { }

        /// <inheritdoc/>
        public TSource? MapFromOData(ODataItem entity, OperationTypes operationType = OperationTypes.Unspecified)
        {
            entity.ThrowIfNull(nameof(entity));
            var value = new TSource();

            foreach (var p in _mappings)
            {
                p.MapFromOData(entity, value, operationType);
            }

            value = OnMapFromOData(value, entity, operationType);
            return (value != null && value is IInitial ii && ii.IsInitial) ? null : value;
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapFromOData(ODataItem, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="entity">The <see cref="ODataItem"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The source value.</returns>
        protected virtual TSource? OnMapFromOData(TSource value, ODataItem entity, OperationTypes operationType) => value;

        /// <summary>
        /// <typeparamref name="TSource"/> to <see cref="ODataItem"/> mapper.
        /// </summary>
        private class SourceToItemMapper(ODataMapper<TSource> parent) : Mapper<TSource, ODataItem>
        {
            internal ODataMapper<TSource> Parent { get; } = parent;

            protected override ODataItem? OnMap(TSource? source, ODataItem? destination, OperationTypes operationType)
            {
                if (source is null)
                    return default;

                destination ??= new ODataItem();
                Parent.MapToOData(source, destination, operationType);
                return destination;
            }
        }

        /// <summary>
        /// <see cref="ODataItem"/> to <typeparamref name="TSource"/> mapper.
        /// </summary>
        private class ItemToSourceMapper(ODataMapper<TSource> parent) : Mapper<ODataItem, TSource>
        {
            internal ODataMapper<TSource> Parent { get; } = parent;

            protected override TSource? OnMap(ODataItem? source, TSource? destination, OperationTypes operationType)
            {
                if (source is null)
                    return default;

                return Parent.MapFromOData(source, operationType);
            }
        }

        /// <inheritdoc/>
        IMapper<TSource, ODataItem> IBidirectionalMapper<TSource, ODataItem>.MapperFromTo => _mapperFromTo.Value;

        /// <inheritdoc/>
        IMapper<ODataItem, TSource> IBidirectionalMapper<TSource, ODataItem>.MapperToFrom => _mapperToFrom.Value;

        /// <inheritdoc/>
        public object[] GetODataKey(TSource value, OperationTypes operationType = OperationTypes.Unspecified)
        {
            value.ThrowIfNull(nameof(value));

            var km = _mappings.Where(x => x.IsPrimaryKey).ToArray();
            if (km.Length == 0)
                throw new InvalidOperationException("No primary key mappings have been defined.");

            var oi = new ODataItem();
            foreach (var p in km)
            {
                p.MapToOData(value, oi, operationType);
            }

            var key = new object[km.Length];
            for (int i = 0; i < km.Length; i++)
            {
                key[i] = oi.Attributes[km[i].ColumnName];
            }

            return key;
        }
    }
}