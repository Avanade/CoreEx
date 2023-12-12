// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping;
using CoreEx.OData.Mapping;
using System;
using System.Collections.Generic;

namespace CoreEx.OData
{
    /// <summary>
    /// Provides a untyped <see cref="Attributes"/>-based (<see cref="IDictionary{TKey, TValue}"/>) <b>OData</b> item/entry.
    /// </summary>
    public class ODataItem
    {
        /// <summary>
        /// Maps none or more <paramref name="items"/> into a corresponding <see cref="ODataItem"/> <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="items">The OData items.</param>
        /// <returns>The <see cref="ODataItem"/> <see cref="IEnumerable{T}"/>.</returns>
        public static IEnumerable<ODataItem> MapODataItems(IEnumerable<IDictionary<string, object>> items)
        {
            foreach (var item in items)
            {
                yield return new ODataItem(item);
            }
        }

        /// <summary>
        /// Maps none or more <paramref name="items"/> into a corresponding <typeparamref name="T"/> <see cref="IEnumerable{T}"/> using the specified <paramref name="mapper"/>.
        /// </summary>
        /// <typeparam name="T">The resulting item <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The specific <see cref="IODataMapper{TSource}"/>.</param>
        /// <param name="items">The OData items.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The <typeparamref name="T"/> <see cref="IEnumerable{T}"/>.</returns>
        public static IEnumerable<T> MapODataItems<T>(IODataMapper<T> mapper, IEnumerable<IDictionary<string, object>> items, OperationTypes operationType = OperationTypes.Unspecified)
        {
            foreach (var item in MapODataItems(items))
            {
                yield return mapper.MapFromOData(item, operationType)!;
            }
        }

        /// <summary>
        /// Creates an <see cref="ODataItem"/> from the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The specific <see cref="IODataMapper{TSource}"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public static ODataItem MapFrom<T>(IODataMapper<T> mapper, T value, OperationTypes operationType = OperationTypes.Unspecified)
        {
            var result = new ODataItem();
            mapper.MapToOData(value, result, operationType);
            return result;
        }

        /// <summary>
        /// Creates an <see cref="ODataItem"/> from the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The specific <see cref="IMapper{TSource, TDestination}"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public static ODataItem? MapFrom<T>(IMapper<T, ODataItem> mapper, T? value, OperationTypes operationType = OperationTypes.Unspecified) => mapper.Map(value, operationType);

        /// <summary>
        /// Creates an <see cref="ODataItem"/> from the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IMapper{TSource, TDestination}"/> mapper.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public static ODataItem? MapFrom<T>(IMapper mapper, T? value, OperationTypes operationType = OperationTypes.Unspecified) => mapper.Map<T, ODataItem>(value, operationType);

        /// <summary>
        /// Creates an <see cref="ODataItem"/> from the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="client">The <see cref="ODataClient"/> mapper.</param>
        /// <param name="value">The value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public static ODataItem? MapFrom<T>(ODataClient client, T? value, OperationTypes operationType = OperationTypes.Unspecified) => MapFrom(client.Mapper, value, operationType);

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataItem"/> class.
        /// </summary>
        public ODataItem() => Attributes = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataItem"/> class with the specified <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The <see cref="IDictionary{TKey, TValue}"/>.</param>
        public ODataItem(IDictionary<string, object> attributes) => Attributes = attributes;

        /// <summary>
        /// Gets the attributes/columns.
        /// </summary>
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the value of the specified attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The value.</returns>
        public object? this[string name]
        {
            get => Attributes[name];

            set
            {
                if (Attributes.ContainsKey(name))
                    Attributes[name] = value!;
                else
                    Attributes.Add(name, value!);
            }
        }

        /// <summary>
        /// Maps the <see cref="ODataItem"/> to a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The specific <see cref="IODataMapper{TSource}"/>.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public T MapTo<T>(IODataMapper<T> mapper, OperationTypes operationType = OperationTypes.Unspecified) where T : class, new() => mapper.MapFromOData(this, operationType)!;

        /// <summary>
        /// Maps the <see cref="ODataItem"/> to a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The specific <see cref="IMapper{TSource, TDestination}"/>.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public T MapTo<T>(IMapper<ODataItem, T> mapper, OperationTypes operationType = OperationTypes.Unspecified) => mapper.Map(this, operationType)!;

        /// <summary>
        /// Maps the <see cref="ODataItem"/> to a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
        /// <param name="mapper">The <see cref="IMapper"/>.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public T MapTo<T>(IMapper mapper, OperationTypes operationType = OperationTypes.Unspecified) => mapper.Map<ODataItem, T>(this, operationType)!;

        /// <summary>
        /// Maps the <see cref="ODataItem"/> to a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
        /// <param name="client">The <see cref="ODataClient"/>.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The resultant value.</returns>
        public T MapTo<T>(ODataClient client, OperationTypes operationType = OperationTypes.Unspecified) => MapTo<T>(client.Mapper, operationType);
    }
}