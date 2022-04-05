// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Represents the <see cref="CollectionReflector"/> <see cref="Type"/> type.
    /// </summary>
    public enum CollectionReflectorType
    {
        /// <summary>
        /// Is an <see cref="object"/> (not identified as one of the possible collection types).
        /// </summary>
        Object,

        /// <summary>
        /// Is an <see cref="System.Array"/>.
        /// </summary>
        Array,

        /// <summary>
        /// Is an <see cref="System.Collections.ICollection"/>.
        /// </summary>
        ICollection,

        /// <summary>
        /// Is an <see cref="System.Collections.IEnumerable"/>.
        /// </summary>
        IEnumerable,

        /// <summary>
        /// Is an <see cref="System.Collections.IDictionary"/>.
        /// </summary>
        IDictionary
    }

    /// <summary>
    /// Utility reflection class for identifying, creating and updating collections.
    /// </summary>
    public class CollectionReflector
    {
        private IInternalEqualityComparer? _equalityComparer;

        /// <summary>
        /// Private constructor.
        /// </summary>
        private CollectionReflector(PropertyInfo propertyInfo, Type itemType)
        {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
        }

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/>.
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets or sets the <see cref="ComplexTypeCode"/>.
        /// </summary>
        public CollectionReflectorType ComplexTypeCode { get; set; }

        /// <summary>
        /// Gets or sets the collection item <see cref="Type"/> where <see cref="IsCollection"/>; otherwise, the <see cref="PropertyInfo.PropertyType"/>.
        /// </summary>
        public Type ItemType { get; private set; }

        /// <summary>
        /// Indicates whether the <see cref="ItemType"/> is considered a complex type.
        /// </summary>
        public bool IsItemComplexType { get; private set; }

        /// <summary>
        /// Gets the KeyValuePair <see cref="Type"/> where the <see cref="ComplexTypeCode"/> is <see cref="CollectionReflectorType.IDictionary"/>.
        /// </summary>
        public Type? DictKeyValuePairType { get; private set; }

        /// <summary>
        /// Gets the key <see cref="Type"/> where the <see cref="ComplexTypeCode"/> is <see cref="CollectionReflectorType.IDictionary"/>.
        /// </summary>
        public Type? DictKeyType { get; private set; }

        /// <summary>
        /// Indicates whether the <see cref="ComplexTypeCode"/> is a collection of some description.
        /// </summary>
        public bool IsCollection => ComplexTypeCode != CollectionReflectorType.Object;

        /// <summary>
        /// Gets the <b>Add</b> (where available) method for an <see cref="IsCollection"/>.
        /// </summary>
        public MethodInfo? AddMethod { get; private set; }

        /// <summary>
        /// Gets the instantiation type for an <see cref="IsCollection"/>.
        /// </summary>
        private Type? CollectionInstantiateType { get; set; }

        /// <summary>
        /// Creates a <see cref="CollectionReflector"/> from a <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="pi">The <see cref="PropertyInfo"/>.</param>
        /// <returns>The <see cref="CollectionReflector"/>.</returns>
        public static CollectionReflector Create(PropertyInfo pi)
        {
            var tr = new CollectionReflector(pi ?? throw new ArgumentNullException(nameof(pi)), pi.PropertyType);

            if (pi.PropertyType == typeof(string) || pi.PropertyType.IsPrimitive || pi.PropertyType.IsValueType)
                return tr;

            if (pi.PropertyType.IsArray)
            {
                tr.ComplexTypeCode = CollectionReflectorType.Array;
                tr.ItemType = pi.PropertyType.GetElementType();
            }
            else
            {
                if (pi.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)))
                {
                    var (keyType, valueType) = GetDictionaryType(pi.PropertyType);
                    if (keyType != null)
                    {
                        tr.ComplexTypeCode = CollectionReflectorType.IDictionary;
                        tr.DictKeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                        tr.DictKeyType = keyType!;
                        tr.ItemType = valueType!;
                        tr.AddMethod = pi.PropertyType.GetMethod("Add", new Type[] { tr.DictKeyType, tr.ItemType });
                        tr.CollectionInstantiateType = pi.PropertyType.IsInterface ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : pi.PropertyType;
                    }
                    else
                    {
                        var t = GetCollectionType(pi.PropertyType);
                        if (t != null)
                        {
                            tr.ComplexTypeCode = CollectionReflectorType.ICollection;
                            tr.ItemType = t;
                            tr.AddMethod = pi.PropertyType.GetMethod("Add", new Type[] { t });
                            if (tr.AddMethod == null)
                                throw new ArgumentException($"Type '{pi.DeclaringType.Name}' Property '{pi.Name}' is an ICollection<>; however, no Add method could be found.", nameof(pi));

                            tr.CollectionInstantiateType = pi.PropertyType.IsInterface ? typeof(List<>).MakeGenericType(t) : pi.PropertyType;
                        }
                        else
                        {
                            t = GetEnumerableType(pi.PropertyType);
                            if (t != null)
                            {
                                tr.ComplexTypeCode = CollectionReflectorType.IEnumerable;
                                tr.ItemType = t;
                            }
                            else
                            {
                                var result = GetEnumerableTypeFromAdd(pi.PropertyType);
                                if (result.ItemType != null)
                                {
                                    tr.ComplexTypeCode = CollectionReflectorType.ICollection;
                                    tr.ItemType = result.ItemType;
                                    tr.AddMethod = result.AddMethod;
                                }
                            }
                        }
                    }
                }
            }

            if (tr.ItemType != null)
                tr.IsItemComplexType = !(tr.ItemType == typeof(string) || tr.ItemType.IsPrimitive || tr.ItemType.IsValueType);

            tr._equalityComparer = (IInternalEqualityComparer)Activator.CreateInstance(typeof(InternalEqualityComparer<>).MakeGenericType(tr.ComplexTypeCode == CollectionReflectorType.IDictionary ? tr.DictKeyValuePairType : tr.ItemType));
            return tr;
        }

        /// <summary>
        /// Gets the underlying item <see cref="Type"/> where an <see cref="Array"/>, <see cref="IDictionary"/>, <see cref="ICollection"/> or <see cref="IEnumerable"/>; otherwise itself (<paramref name="type"/>). 
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The item <see cref="Type"/> or itself.</returns>
        public static Type GetItemType(Type type)
        {
            if ((type ?? throw new ArgumentNullException(nameof(type))) == typeof(string) || type.IsPrimitive || type.IsValueType)
                return type;

            if (type.IsArray)
                return type.GetElementType();

            var (_, valueType) = GetDictionaryType(type);
            if (valueType != null)
                return valueType;

            var t = GetCollectionType(type);
            if (t != null)
                return t;

            t = GetEnumerableType(type);
            if (t != null)
                return t;

            var result = GetEnumerableTypeFromAdd(type);
            if (result.ItemType != null)
                return result.ItemType;

            return type;
        }

        /// <summary>
        /// Gets the underlying ICollection Type.
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
        /// Gets the underlying IEnumerable Type.
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
        /// Gets the underlying IDictionary Type.
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
        /// Gets the underlying IEnumerable Type by inferring from the Add method.
        /// </summary>
        private static (Type? ItemType, MethodInfo? AddMethod) GetEnumerableTypeFromAdd(Type type)
        {
            var mi = type.GetMethod("Add");
            if (mi == null)
                return (null, null);

            var ps = mi.GetParameters();
            return ps.Length == 1 ? (ps[0].ParameterType, mi) : (null, null);
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="objValue">The object whose property value will be set.</param>
        /// <param name="value">The property value(s) to set.</param>
        public void SetValue(object? objValue, IEnumerable? value)
        {
            if (objValue == null || value == null)
                return;

            PropertyInfo.SetValue(objValue, CreateValue(value));
        }

        /// <summary>
        /// Creates the <see cref="IsCollection"/> as empty (i.e. instantiates only); otherise, default.
        /// </summary>
        /// <returns>The value.</returns>
        public object? CreateValue()
        {
            if (PropertyInfo.PropertyType == typeof(string))
                return null;

            return ComplexTypeCode switch
            {
                CollectionReflectorType.Object => IsItemComplexType ? null : Activator.CreateInstance(PropertyInfo.PropertyType),
                CollectionReflectorType.ICollection or CollectionReflectorType.IDictionary => Activator.CreateInstance(CollectionInstantiateType),
                _ => Array.CreateInstance(ItemType, 0),
            };
        }

        /// <summary>
        /// Creates the property value from a list/array/collection.
        /// </summary>
        /// <param name="value">The property value(s) to set.</param>
        /// <returns>The property value.</returns>
        public object CreateValue(IEnumerable value)
        {
            IList? a = null;
            Type? aType = null;
            object? c = null;

            if (IsCollection)
            {
                switch (ComplexTypeCode)
                {
                    case CollectionReflectorType.Array:
                    case CollectionReflectorType.IEnumerable:
                        aType = typeof(List<>).MakeGenericType(ItemType);
                        a = (IList)Activator.CreateInstance(aType);
                        break;

                    case CollectionReflectorType.ICollection:
                        c = Activator.CreateInstance(CollectionInstantiateType);
                        break;

                    case CollectionReflectorType.IDictionary:
                        return value;
                }
            }

            if (value != null)
            {
                foreach (var val in value)
                {
                    if (!IsCollection)
                        return val;

                    switch (ComplexTypeCode)
                    {
                        case CollectionReflectorType.Array:
                        case CollectionReflectorType.IEnumerable:
                            a!.Add(val);
                            break;

                        case CollectionReflectorType.ICollection:
                            AddMethod!.Invoke(c, new object[] { val });
                            break;
                    }
                }
            }

            if (a != null)
                return aType!.GetMethod("ToArray").Invoke(a, null);
            else if (c != null)
                return c;
            else
                return null!;
        }

        /// <summary>
        /// Creates an instance of the item value.
        /// </summary>
        /// <returns>An instance of the item value.</returns>
        public object? CreateItemValue() => ItemType == typeof(string) ? null : Activator.CreateInstance(ItemType);

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The second value.</param>
        /// <returns><c>true</c> if the two source sequences are of equal length and their corresponding elements are equal according to the default equality comparer for their type; otherwise, <c>false</c>.</returns>
        public bool CompareSequence(object? left, object? right)
        {
            if (ComplexTypeCode == CollectionReflectorType.Object)
                throw new InvalidOperationException("CompareSequence cannot be performed for a ComplexTypeCode.Object.");

            if (left == null && right == null)
                return true;

            if ((left != null && right == null) || (left == null && right != null))
                return false;

            if (left == right)
                return true;

            switch (ComplexTypeCode)
            {
                case CollectionReflectorType.Array:
                    var al = (Array)left!;
                    var ar = (Array)right!;
                    if (al.Length != ar.Length)
                        return false;

                    break;

                case CollectionReflectorType.ICollection:
                    var cl = (ICollection)left!;
                    var cr = (ICollection)right!;
                    if (cl.Count != cr.Count)
                        return false;

                    break;

                case CollectionReflectorType.IDictionary:
                    var dl = (IDictionary)left!;
                    var dr = (IDictionary)right!;
                    if (dl.Count != dr.Count)
                        return false;

                    break;
            }

            // Inspired by: https://referencesource.microsoft.com/#System.Core/System/Linq/Enumerable.cs,9bdd6ef7ba6a5615
            var el = ((IEnumerable)left!).GetEnumerator();
            var er = ((IEnumerable)right!).GetEnumerator();
            {
                while (el.MoveNext())
                {
                    if (!(er.MoveNext() && _equalityComparer!.IsEqual(el.Current, er.Current))) return false;
                }
                if (er.MoveNext()) return false;
            }

            return true;
        }

        /// <summary>
        /// Enables a non-generics equality comparer.
        /// </summary>
        private interface IInternalEqualityComparer
        {
            bool IsEqual(object x, object y);
        }

        /// <summary>
        /// Provides the non-generics equality generic comparer; leveraging the generics comparer within.
        /// </summary>
        private class InternalEqualityComparer<T> : IInternalEqualityComparer
        {
            public bool IsEqual(object x, object y) => EqualityComparer<T>.Default.Equals((T)x, (T)y);
        }
    }
}
