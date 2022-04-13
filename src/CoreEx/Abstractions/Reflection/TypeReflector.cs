// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Utility reflection class for identifying, creating and updating collections.
    /// </summary>
    public class TypeReflector
    {
        private IInternalEqualityComparer? _equalityComparer;

        /// <summary>
        /// Private constructor.
        /// </summary>
        private TypeReflector(PropertyInfo propertyInfo, Type itemType)
        {
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
            CollectionItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
        }

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/>.
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// Gets or sets the <see cref="TypeReflectorTypeCode"/>.
        /// </summary>
        public TypeReflectorTypeCode TypeCode { get; private set; } = TypeReflectorTypeCode.Complex;

        /// <summary>
        /// Gets or sets the collection item <see cref="Type"/> where <see cref="IsCollectionType"/>; otherwise, the <see cref="PropertyInfo.PropertyType"/>.
        /// </summary>
        public Type CollectionItemType { get; private set; }

        /// <summary>
        /// Indicates whether the <see cref="CollectionItemType"/> is considered a complex type.
        /// </summary>
        public bool IsCollectionItemAComplexType { get; private set; }

        /// <summary>
        /// Gets the KeyValuePair <see cref="Type"/> where the <see cref="TypeCode"/> is <see cref="TypeReflectorTypeCode.IDictionary"/>.
        /// </summary>
        public Type? DictKeyValuePairType { get; private set; }

        /// <summary>
        /// Gets the key <see cref="Type"/> where the <see cref="TypeCode"/> is <see cref="TypeReflectorTypeCode.IDictionary"/>.
        /// </summary>
        public Type? DictKeyType { get; private set; }

        /// <summary>
        /// Indicates whether the <see cref="TypeCode"/> is a collection of some description.
        /// </summary>
        public bool IsCollectionType => TypeCode != TypeReflectorTypeCode.Complex && TypeCode != TypeReflectorTypeCode.Simple;

        /// <summary>
        /// Gets the <b>Add</b> (where available) method for an <see cref="IsCollectionType"/>.
        /// </summary>
        public MethodInfo? CollectionAddMethod { get; private set; }

        /// <summary>
        /// Gets the instantiation type for an <see cref="IsCollectionType"/>.
        /// </summary>
        private Type? CollectionInstantiateType { get; set; }

        /// <summary>
        /// Creates a <see cref="TypeReflector"/> from a <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="pi">The <see cref="PropertyInfo"/>.</param>
        /// <returns>The <see cref="TypeReflector"/>.</returns>
        public static TypeReflector Create(PropertyInfo pi)
        {
            var tr = new TypeReflector(pi ?? throw new ArgumentNullException(nameof(pi)), pi.PropertyType);

            if (pi.PropertyType == typeof(string) || pi.PropertyType.IsPrimitive || pi.PropertyType.IsValueType)
            {
                tr.TypeCode = TypeReflectorTypeCode.Simple;
                return tr;
            }

            if (pi.PropertyType.IsArray)
            {
                tr.TypeCode = TypeReflectorTypeCode.Array;
                tr.CollectionItemType = pi.PropertyType.GetElementType();
            }
            else
            {
                if (pi.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)))
                {
                    var (keyType, valueType) = GetDictionaryType(pi.PropertyType);
                    if (keyType != null)
                    {
                        tr.TypeCode = TypeReflectorTypeCode.IDictionary;
                        tr.DictKeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                        tr.DictKeyType = keyType!;
                        tr.CollectionItemType = valueType!;
                        tr.CollectionAddMethod = pi.PropertyType.GetMethod("Add", new Type[] { tr.DictKeyType, tr.CollectionItemType });
                        tr.CollectionInstantiateType = pi.PropertyType.IsInterface ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : pi.PropertyType;
                    }
                    else
                    {
                        var t = GetCollectionType(pi.PropertyType);
                        if (t != null)
                        {
                            tr.TypeCode = TypeReflectorTypeCode.ICollection;
                            tr.CollectionItemType = t;
                            tr.CollectionAddMethod = pi.PropertyType.GetMethod("Add", new Type[] { t });
                            if (tr.CollectionAddMethod == null)
                                throw new ArgumentException($"Type '{pi.DeclaringType.Name}' Property '{pi.Name}' is an ICollection<>; however, no Add method could be found.", nameof(pi));

                            tr.CollectionInstantiateType = pi.PropertyType.IsInterface ? typeof(List<>).MakeGenericType(t) : pi.PropertyType;
                        }
                        else
                        {
                            t = GetEnumerableType(pi.PropertyType);
                            if (t != null)
                            {
                                tr.TypeCode = TypeReflectorTypeCode.IEnumerable;
                                tr.CollectionItemType = t;
                            }
                            else
                            {
                                var (ItemType, AddMethod) = GetEnumerableTypeFromAdd(pi.PropertyType);
                                if (ItemType != null)
                                {
                                    tr.TypeCode = TypeReflectorTypeCode.ICollection;
                                    tr.CollectionItemType = ItemType;
                                    tr.CollectionAddMethod = AddMethod;
                                }
                            }
                        }
                    }
                }
            }

            if (tr.CollectionItemType != null)
                tr.IsCollectionItemAComplexType = !(tr.CollectionItemType == typeof(string) || tr.CollectionItemType.IsPrimitive || tr.CollectionItemType.IsValueType);

            tr._equalityComparer = (IInternalEqualityComparer)Activator.CreateInstance(typeof(InternalEqualityComparer<>).MakeGenericType(tr.TypeCode == TypeReflectorTypeCode.IDictionary ? tr.DictKeyValuePairType : tr.CollectionItemType));
            return tr;
        }

        /// <summary>
        /// Gets the underlying item <see cref="Type"/> where an <see cref="Array"/>, <see cref="IDictionary"/>, <see cref="ICollection"/> or <see cref="IEnumerable"/>. 
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The collection item <see cref="Type"/> where <see cref="IsCollectionType"/>; otherwise, <c>null</c>.</returns>
        public static Type? GetCollectionItemType(Type type)
        {
            if ((type ?? throw new ArgumentNullException(nameof(type))) == typeof(string) || type.IsPrimitive || type.IsValueType)
                return null;

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

            var (ItemType, _) = GetEnumerableTypeFromAdd(type);
            if (ItemType != null)
                return ItemType;

            return null;
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

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="objValue">The object whose property value will be set.</param>
        /// <param name="value">The property value(s) to set.</param>
        public void SetCollectionValue(object? objValue, IEnumerable? value)
        {
            if (!IsCollectionType || objValue == null || value == null)
                return;

            PropertyInfo.SetValue(objValue, CreateCollectionValue(value));
        }

        /// <summary>
        /// Creates the <see cref="IsCollectionType"/> as empty (i.e. instantiates only).
        /// </summary>
        /// <returns>The collection as empty.</returns>
        public object? CreateCollectionValue()
        {
            if (!IsCollectionType)
                throw new InvalidOperationException($"Must be an {nameof(IsCollectionType)} to use this method.");

            return TypeCode switch
            {
                TypeReflectorTypeCode.ICollection or TypeReflectorTypeCode.IDictionary => Activator.CreateInstance(CollectionInstantiateType),
                _ => Array.CreateInstance(CollectionItemType, 0),
            };
        }

        /// <summary>
        /// Creates the <see cref="IsCollectionType"/> value from an existing <see cref="IEnumerable"/> value.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The collection with included <paramref name="values"/>.</returns>
        public object CreateCollectionValue(IEnumerable values)
        {
            if (!IsCollectionType)
                throw new InvalidOperationException($"Must be an {nameof(IsCollectionType)} to use this method.");

            IList? a = null;
            Type? aType = null;
            object? c = null;

            switch (TypeCode)
            {
                case TypeReflectorTypeCode.Array:
                case TypeReflectorTypeCode.IEnumerable:
                    aType = typeof(List<>).MakeGenericType(CollectionItemType);
                    a = (IList)Activator.CreateInstance(aType);
                    break;

                case TypeReflectorTypeCode.ICollection:
                    c = Activator.CreateInstance(CollectionInstantiateType);
                    break;

                case TypeReflectorTypeCode.IDictionary:
                    return values;
            }

            if (values != null)
            {
                foreach (var val in values)
                {
                    if (!IsCollectionType)
                        return val;

                    switch (TypeCode)
                    {
                        case TypeReflectorTypeCode.Array:
                        case TypeReflectorTypeCode.IEnumerable:
                            a!.Add(val);
                            break;

                        case TypeReflectorTypeCode.ICollection:
                            CollectionAddMethod!.Invoke(c, new object[] { val });
                            break;
                    }
                }
            }

            if (a != null)
                return aType!.GetMethod("ToArray").Invoke(a, null);
            else
                return c ?? throw new InvalidOperationException("An invalid non-null value has unexpectantly been created.");
        }

        /// <summary>
        /// Creates an instance of the item value.
        /// </summary>
        /// <returns>An instance of the item value.</returns>
        public object? CreateCollectionItemValue() => CollectionItemType == typeof(string) ? null : Activator.CreateInstance(CollectionItemType);

        /// <summary>
        /// Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The second value.</param>
        /// <returns><c>true</c> if the two source sequences are of equal length and their corresponding elements are equal according to the default equality comparer for their type; otherwise, <c>false</c>.</returns>
        public bool CompareSequence(object? left, object? right)
        {
            if (TypeCode == TypeReflectorTypeCode.Complex)
                throw new InvalidOperationException("CompareSequence cannot be performed for a ComplexTypeCode.Object.");

            if (left == null && right == null)
                return true;

            if ((left != null && right == null) || (left == null && right != null))
                return false;

            if (left == right)
                return true;

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