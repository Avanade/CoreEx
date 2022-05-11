// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
        /// <param name="args">The <see cref="TypeReflectorArgs"/>.</param>
        internal TypeReflector(TypeReflectorArgs? args = null)
        {
            Args = args ?? new TypeReflectorArgs();
            _properties = new Dictionary<string, IPropertyReflector>(StringComparer.Ordinal);
            _jsonProperties = new Dictionary<string, IPropertyReflector>(Args.NameComparer ?? StringComparer.OrdinalIgnoreCase);

            var tr = GetCollectionItemType(Type);
            ItemType = tr.ItemType;
            TypeCode = tr.TypeCode;
            if (ItemType != null)
                ItemTypeCode = GetCollectionItemType(ItemType).TypeCode;

            if (!Args.AutoPopulateProperties)
                return;

            var pe = Expression.Parameter(typeof(TEntity), "x");

            foreach (var p in TypeReflector.GetProperties(typeof(TEntity)))
            {
                var lex = Expression.Lambda(Expression.Property(pe, p.Name), pe);
                var pr = (IPropertyReflector)Activator.CreateInstance(typeof(PropertyReflector<,>).MakeGenericType(typeof(TEntity), p.PropertyType), Args, lex);

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

        /// <inheritdoc/>
        public ITypeReflector? GetItemTypeReflector() => _itemReflector ??= TypeReflector.GetReflector(Args, ItemType!);

        #region Collections

        /// <summary>
        /// Gets the underlying item <see cref="Type"/> where an <see cref="Array"/>, <see cref="IDictionary"/>, <see cref="ICollection"/> or <see cref="IEnumerable"/>. 
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="TypeReflectorTypeCode"/> and corresponding item <see cref="Type"/> where a collection.</returns>
        private static (TypeReflectorTypeCode TypeCode, Type? ItemType) GetCollectionItemType(Type type)
        {
            if ((type ?? throw new ArgumentNullException(nameof(type))) == typeof(string) || type.IsPrimitive || type.IsValueType)
                return (TypeReflectorTypeCode.Simple, null);

            if (type.IsArray)
                return (TypeReflectorTypeCode.Array, type.GetElementType());

            var (_, valueType) = GetDictionaryType(type);
            if (valueType != null)
                return (TypeReflectorTypeCode.IDictionary, valueType);

            var t = GetCollectionType(type);
            if (t != null)
                return (TypeReflectorTypeCode.ICollection, t);

            t = GetEnumerableType(type);
            if (t != null)
                return (TypeReflectorTypeCode.IEnumerable, t);

            var (ItemType, _) = GetEnumerableTypeFromAdd(type);
            if (ItemType != null)
                return (TypeReflectorTypeCode.IEnumerable, ItemType);

            return (TypeReflectorTypeCode.Complex, null);
        }

        /// <summary>
        /// Gets the underlying <see cref="ICollection{T}"/> Type.
        /// </summary>
        private static Type? GetCollectionType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
                return type.GetGenericArguments()[0];

            var t = type.GetInterfaces().FirstOrDefault(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (t == null)
                return null;

            return ((t == typeof(ICollection<>)) ? type : t).GetGenericArguments()[0];
        }

        /// <summary>
        /// Gets the underlying <see cref="IEnumerable"/> Type.
        /// </summary>
        private static Type? GetEnumerableType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            var t = type.GetInterfaces().FirstOrDefault(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (t == null)
            {
                t = type.GetInterfaces().FirstOrDefault(x => x == typeof(IEnumerable));
                if (t == null)
                    return null;
            }

            var gas = ((t == typeof(IEnumerable)) ? type : t).GetGenericArguments();
            if (gas.Length == 0)
                return null;

            if (type == typeof(IEnumerable<>).MakeGenericType(new Type[] { gas[0] }))
                return gas[0];

            return null;
        }

        /// <summary>
        /// Gets the underlying <see cref="IDictionary{TKey, TValue}"/> Type.
        /// </summary>
        private static (Type? keyType, Type? valueType) GetDictionaryType(Type type)
        {
            Type? t;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                t = type;
            else
                t = type.GetInterfaces().FirstOrDefault(x => (x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>)));

            if (t == null)
                return (null, null);

            var gas = t.GetGenericArguments();
            if (gas.Length != 2)
                return (null, null);

            return (gas[0], gas[1]);
        }

        /// <summary>
        /// Gets the underlying <see cref="IEnumerable"/> Type by inferring from the Add method.
        /// </summary>
        private static (Type? ItemType, MethodInfo? AddMethod) GetEnumerableTypeFromAdd(Type type)
        {
            var mi = type.GetMethod("Add");
            if (mi == null)
                return (null, null);

            var ps = mi.GetParameters();
            return ps.Length == 1 ? (ps[0].ParameterType, mi) : (null, null);
        }

        #endregion

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
            _itemEqualityComparer ??= (IItemEqualityComparer)Activator.CreateInstance(typeof(ItemEqualityComparer<>).MakeGenericType(ItemType!));

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

                        if (!_itemEqualityComparer!.IsEqual(edl.Value, dr[edl.Key]))
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