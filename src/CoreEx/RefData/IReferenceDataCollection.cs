// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides <see cref="GetById(object)"/> and <see cref="GetByCode(string)"/> functionality for an <see cref="IReferenceData"/> collection.
    /// </summary>
    public interface IReferenceDataCollection
    {
        /// <summary>
        /// Adds the <see cref="IReferenceData"/> to the <see cref="IReferenceDataCollection"/>.
        /// </summary>
        /// <param name="item">The <see cref="IReferenceData"/>.</param>
        void Add(IReferenceData item);

        /// <summary>
        /// Adds the <paramref name="collection"/> of items to the <see cref="IReferenceDataCollection"/>.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        void AddRange(IEnumerable<IReferenceData> collection);

        /// <summary>
        /// Determines whether the specified <see cref="IIdentifier.Id"/> exists within the collection.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifier.Id"/>.</param>
        /// <returns><c>true</c> if it exists; otherwise, <c>false</c>.</returns>
        bool ContainsId(object id);

        /// <summary>
        /// Determines whether the specified <see cref="IReferenceData.Code"/> exists within the collection.
        /// </summary>
        /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
        /// <returns><c>true</c> if it exists; otherwise, <c>false</c>.</returns>
        bool ContainsCode(string code);

        /// <summary>
        /// Attempts to get the <paramref name="item"/> with the specifed <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifier.Id"/>.</param>
        /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetById(object id, out IReferenceData? item);

        /// <summary>
        /// Attempts to get the <paramref name="item"/> with the specifed <paramref name="code"/>.
        /// </summary>
        /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
        /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetByCode(string code, out IReferenceData? item);

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IIdentifier.Id"/>.
        /// </summary>
        /// <param name="id">The specified reference data <see cref="IIdentifier.Id"/>.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        IReferenceData? GetById(object id);

        /// <summary>
        /// Gets the<see cref="IReferenceData"/> for the specified <see cref="IReferenceData.Code"/>.
        /// </summary>
        /// <param name="code">The specified <see cref="IReferenceData.Code"/>.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        IReferenceData? GetByCode(string code);

        /// <summary>
        /// Determines whether the specified <see cref="IReferenceData.GetMapping{T}(string)"/> value exists within the collection.
        /// </summary>
        /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <returns><c>true</c> if it exists; otherwise, <c>false</c>.</returns>
        bool ContainsMappingValue<T>(string name, T value) where T : IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Attempts to get the <paramref name="item"/> with the specifed <see cref="IReferenceData.GetMapping{T}(string)"/> value.
        /// </summary>
        /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetByMappingValue<T>(string name, T value, out IReferenceData? item) where T : IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IReferenceData.GetMapping{T}(string)"/> value.
        /// </summary>
        /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        IReferenceData? GetByMappingValue<T>(string name, T value) where T : IComparable<T>, IEquatable<T>;
    }
}