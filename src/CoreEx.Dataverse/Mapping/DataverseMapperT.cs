// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.Mapping;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreEx.Dataverse.Mapping
{
    /// <summary>
    /// Provides mapping from a <typeparamref name="TSource"/> <see cref="Type"/> and <i>Dataverse</i> <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    public class DataverseMapper<TSource> : IDataverseMapper<TSource>, IDataverseMapperMappings where TSource : class, new()
    {
        private readonly List<IPropertyColumnMapper> _mappings = new();
        private readonly bool _implementsIIdentifier = typeof(IIdentifier).IsAssignableFrom(typeof(TSource));

        /// <summary>
        /// Initializes a new instance of the <see cref="DataverseMapper{TSource}"/> class.
        /// </summary>
        /// <param name="autoMap">Indicates whether the entity should automatically map all public get/set properties, where the property and column names are all assumed to share the same name.</param>
        /// <param name="ignoreSrceProperties">An array of source property names to ignore.</param>
        public DataverseMapper(bool autoMap = false, params string[] ignoreSrceProperties)
        {
            if (typeof(TSource) == typeof(string)) throw new InvalidOperationException("TSource must not be a String.");

            if (autoMap)
                AutomagicallyMap(ignoreSrceProperties);
        }

        /// <inheritdoc/>
        IEnumerable<IPropertyColumnMapper> IDataverseMapperMappings.Mappings => _mappings.AsEnumerable();

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
                typeof(DataverseMapper<TSource>)
                    .GetMethod(nameof(AutoProperty), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(new Type[] { sp.PropertyType })
                    .Invoke(this, new object?[] { sex, null, OperationTypes.Any });
            }
        }

        /// <summary>
        /// Adds a <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper with additiional auto-logic.
        /// </summary>
        private PropertyColumnMapper<TSource, TSourceProperty> AutoProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            var pcm  = Property(propertyExpression, columnName, operationTypes);

            // Automatically set primary key where IIdentifier.
            if (_implementsIIdentifier && pcm.PropertyName == nameof(IIdentifier.Id))
                pcm.SetPrimaryKey(pcm.PropertyType == typeof(Guid) || pcm.PropertyType == typeof(Guid?) || pcm.PropertyType == typeof(string));

            return pcm;
        }

        /// <summary>
        /// Adds a <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>Dataverse</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <returns>The <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</returns>
        public PropertyColumnMapper<TSource, TSourceProperty> Property<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes operationTypes = OperationTypes.Any)
        {
            var pcm = new PropertyColumnMapper<TSource, TSourceProperty>(propertyExpression, columnName, operationTypes);
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

            _mappings.Add(pcm);
        }

        /// <summary>
        /// Adds or updates <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/> to the mapper.
        /// </summary>
        /// <typeparam name="TSourceProperty">The source property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the source property.</param>
        /// <param name="columnName">The <i>Dataverse</i> column name. Defaults to <paramref name="propertyExpression"/> name.</param>
        /// <param name="operationTypes">The <see cref="CoreEx.Mapping.OperationTypes"/> selection to enable inclusion or exclusion of property.</param>
        /// <param name="property">An <see cref="Action"/> enabling access to the created <see cref="PropertyColumnMapper{TSource, TSourceProperty}"/>.</param>
        /// <returns></returns>
        /// <remarks>Where updating an existing the <paramref name="columnName"/> and <paramref name="operationTypes"/> where specified will override the previous values.</remarks>
        public DataverseMapper<TSource> HasProperty<TSourceProperty>(Expression<Func<TSource, TSourceProperty>> propertyExpression, string? columnName = null, OperationTypes? operationTypes = null, Action<PropertyColumnMapper<TSource, TSourceProperty>>? property = null)
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
        /// Inherits the property mappings from the selected <paramref name="inheritMapper"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="inheritMapper"/> source <see cref="Type"/>. Must inherit from <typeparamref name="TSource"/>.</typeparam>
        /// <param name="inheritMapper">The <see cref="IDataverseMapper{T}"/> to inherit from. Must also implement <see cref="IDataverseMapperMappings"/>.</param>
        public void InheritPropertiesFrom<T>(IDataverseMapper<T> inheritMapper) where T : class, new()
        {
            if (inheritMapper == null) throw new ArgumentNullException(nameof(inheritMapper));
            if (!typeof(TSource).IsSubclassOf(typeof(T))) throw new ArgumentException($"Type {typeof(TSource).Name} must inherit from {typeof(T).Name}.", nameof(inheritMapper));
            if (inheritMapper is not IDataverseMapperMappings inheritMappings) throw new ArgumentException($"Type {typeof(T).Name} must implement {typeof(IDataverseMapperMappings).Name} to copy the mappings.", nameof(inheritMapper));

            var pe = Expression.Parameter(typeof(TSource), "x");
            var type = typeof(DataverseMapper<>).MakeGenericType(typeof(TSource));

            foreach (var p in inheritMappings.Mappings)
            {
                var lex = Expression.Lambda(Expression.Property(pe, p.PropertyName), pe);
                var pmap = (IPropertyColumnMapper)type
                    .GetMethod("Property", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
                    .MakeGenericMethod(p.PropertyType)
                    .Invoke(this, new object?[] { lex, p.ColumnName, p.OperationTypes })!;

                if (p.IsPrimaryKey)
                    pmap.SetPrimaryKey(p.IsPrimaryKeyUseEntityIdentifier);

                if (p.Converter != null)
                    pmap.SetConverter(p.Converter);

                if (p.Mapper != null)
                    pmap.SetMapper(p.Mapper);
            }
        }

        /// <inheritdoc/>
        public void MapToDataverse(TSource? value, Entity entity, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (value == null) return;

            foreach (var p in _mappings)
            {
                p.MapToDataverse(value, entity, operationType);
            }

            OnMapToDataverse(value, entity, operationType);
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapToDataverse(TSource, Entity, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="entity">The <see cref="Entity"/> to update from the <paramref name="value"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        protected virtual void OnMapToDataverse(TSource value, Entity entity, OperationTypes operationType) { }

        /// <inheritdoc/>
        public TSource? MapFromDataverse(Entity entity, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var value = new TSource();

            foreach (var p in _mappings)
            {
                p.MapFromDataverse(entity, value, operationType);
            }

            value = OnMapFromDataverse(value, entity, operationType);
            return (value != null && value is IInitial ii && ii.IsInitial) ? null : value;
        }

        /// <summary>
        /// Extension opportunity when performing a <see cref="MapFromDataverse(Entity, OperationTypes)"/>.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <param name="entity">The <see cref="Entity"/>.</param>
        /// <param name="operationType">The single <see cref="OperationTypes"/> value being performed to enable conditional execution where appropriate.</param>
        /// <returns>The source value.</returns>
        protected virtual TSource? OnMapFromDataverse(TSource value, Entity entity, OperationTypes operationType) => value;
    }
}