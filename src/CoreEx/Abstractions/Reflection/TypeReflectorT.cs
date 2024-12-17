// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides a reflector for a given <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public class TypeReflector<TEntity> : ITypeReflector
    {
        private readonly Dictionary<string, IPropertyReflector> _properties;
        private readonly Dictionary<string, IPropertyReflector> _jsonProperties;
        private readonly Lazy<Dictionary<string, object?>> _data = new(true);
        private IItemEqualityComparer? _itemEqualityComparer;
        private ITypeReflector? _itemReflector;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeReflector{TEntity}"/> class.
        /// </summary>
        /// <param name="args">The optional <see cref="TypeReflectorArgs"/>. Defaults to <see cref="TypeReflectorArgs.Default"/>.</param>
        internal TypeReflector(TypeReflectorArgs? args = null)
        {
            Args = args ?? TypeReflectorArgs.Default;
            _properties = new Dictionary<string, IPropertyReflector>(StringComparer.Ordinal);
            _jsonProperties = new Dictionary<string, IPropertyReflector>(Args.NameComparer ?? StringComparer.OrdinalIgnoreCase);

            var tr = TypeReflector.GetCollectionItemType(Type);
            ItemType = tr.ItemType;
            TypeCode = tr.TypeCode;
            if (ItemType != null)
                ItemTypeCode = TypeReflector.GetCollectionItemType(ItemType).TypeCode;

            if (!Args.AutoPopulateProperties)
                return;

            var pe = Expression.Parameter(typeof(TEntity), "x");

            foreach (var p in TypeReflector.GetProperties(typeof(TEntity), Args.PropertyBindingFlags))
            {
                var lex = Expression.Lambda(Expression.Property(pe, p), pe);
                var pr = (IPropertyReflector)Activator.CreateInstance(typeof(PropertyReflector<,>).MakeGenericType(typeof(TEntity), p.PropertyType), Args, lex)!;

                if (Args.PropertyBuilder != null && !Args.PropertyBuilder(pr))
                    continue;

                AddProperty(pr);
            }
        }

        /// <inheritdoc/>
        public TypeReflectorArgs Args { get; private set; }

        /// <inheritdoc/>
        public Type Type => typeof(TEntity);

        /// <inheritdoc/>
        public TypeReflectorTypeCode TypeCode { get; }

        /// <inheritdoc/>
        public Type? ItemType { get; }

        /// <inheritdoc/>
        public TypeReflectorTypeCode? ItemTypeCode { get; }

        /// <inheritdoc/>
        public Dictionary<string, object?> Data { get => _data.Value; }

        /// <summary>
        /// Adds a <see cref="PropertyReflector{TEntity, TProperty}"/> to the reflector.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <returns>The <see cref="PropertyReflector{TEntity, TProperty}"/>.</returns>
        public PropertyReflector<TEntity, TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            var pr = new PropertyReflector<TEntity, TProperty>(Args, propertyExpression.ThrowIfNull(nameof(propertyExpression)));
            AddProperty(pr);
            return pr;
        }

        /// <summary>
        /// Adds the <see cref="IPropertyReflector"/> to the underlying property collections.
        /// </summary>
        private void AddProperty(IPropertyReflector propertyReflector)
        {
            if (_properties.ContainsKey(propertyReflector.ThrowIfNull(nameof(propertyReflector)).Name))
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
        public bool TryGetProperty(string name, [NotNullWhen(true)] out IPropertyReflector? property) => _properties.TryGetValue(name, out property);

        /// <inheritdoc/>
        public IPropertyReflector GetProperty(string name)
        {
            _properties.TryGetValue(name, out var value);
            return value ?? throw new ArgumentException($"Property '{name}' not found for type '{Type.Name}'.", nameof(name));
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
        public IEnumerable<IPropertyReflector> GetProperties() => _properties.Values;

        /// <inheritdoc/>
        public ITypeReflector? GetItemTypeReflector() => _itemReflector ??= TypeReflector.GetReflector(Args, ItemType!);

        #region Compare

        /// <inheritdoc/>
        bool ITypeReflector.Compare(object? x, object? y) => Compare((TEntity?)x, (TEntity?)y);

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        public bool Compare(TEntity? x, TEntity? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            var left = x ?? y;
            var right = x == null ? x : y;
            if (left == null || right == null)
                return false;

            if (left is IEquatable<TEntity> eq)
                return eq.Equals(right!);
            else if (ItemTypeCode.HasValue)
                return CompareSequence(x, y);
            else
                return EqualityComparer<TEntity>.Default.Equals(left, right);
        }

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The second value.</param>
        /// <returns><c>true</c> if the two source sequences are of equal length and their corresponding elements are equal according to the default equality comparer for their type; otherwise, <c>false</c>.</returns>
        private bool CompareSequence(object? left, object? right)
        {
            _itemEqualityComparer ??= (IItemEqualityComparer)Activator.CreateInstance(typeof(ItemEqualityComparer<>).MakeGenericType(ItemType!))!;

            switch (TypeCode)
            {
                case TypeReflectorTypeCode.Array:
                    var al = (Array)left!;
                    var ar = (Array)right!;
                    if (al.Length != ar.Length)
                        return false;

                    break;

                case TypeReflectorTypeCode.ICollection:
                    var cl = (ICollection)left!;
                    var cr = (ICollection)right!;
                    if (cl.Count != cr.Count)
                        return false;

                    break;

                case TypeReflectorTypeCode.IDictionary:
                    var dl = (IDictionary)left!;
                    var dr = (IDictionary)right!;
                    if (dl.Count != dr.Count)
                        return false;

                    var edl = dl.GetEnumerator();
                    while (edl.MoveNext())
                    {
                        if (!dr.Contains(edl.Key))
                            return false;

                        if (!_itemEqualityComparer!.IsEqual(edl.Value!, dr[edl.Key]!))
                            return false;
                    }

                    return true;
            }

            // Inspired by: https://referencesource.microsoft.com/#System.Core/System/Linq/Enumerable.cs,9bdd6ef7ba6a5615
            var el = ((IEnumerable)left!).GetEnumerator();
            var er = ((IEnumerable)right!).GetEnumerator();
            while (el.MoveNext())
            {
                if (!(er.MoveNext() && _itemEqualityComparer!.IsEqual(el.Current, er.Current)))
                    return false;
            }

            if (er.MoveNext())
                return false;

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Enables a non-generics equality comparer.
    /// </summary>
    internal interface IItemEqualityComparer
    {
        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        bool IsEqual(object x, object y);
    }

    /// <summary>
    /// Provides the non-generics equality generic comparer; leveraging the generics comparer within.
    /// </summary>
    internal class ItemEqualityComparer<T> : IItemEqualityComparer
    {
        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns><c>true</c> indicates that they are equal; otherwise, <c>false</c>.</returns>
        public bool IsEqual(object x, object y) => EqualityComparer<T>.Default.Equals((T)x, (T)y);
    }
}