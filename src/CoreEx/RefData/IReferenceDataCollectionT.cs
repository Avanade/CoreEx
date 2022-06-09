// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides <see cref="GetById(TId?)"/> functionality for an <see cref="IReferenceData{TId}"/> collection with a typed <see cref="IIdentifier{T}.Id"/>.
    /// </summary>
    public interface IReferenceDataCollection<TId, TRef> : IReferenceDataCollection where TId : IComparable<TId>, IEquatable<TId> where TRef : class, IReferenceData<TId>
    {
        /// <inheritdoc/>
        Type IReferenceDataCollection.ItemType => typeof(TRef);

        /// <inheritdoc/>
        void IReferenceDataCollection.Add(IReferenceData item) => Add((TRef)item);

        /// <inheritdoc/>
        void IReferenceDataCollection.AddRange(IEnumerable<IReferenceData> collection) => AddRange((IEnumerable<TRef>)collection);
        
        /// <inheritdoc/>
        bool IReferenceDataCollection.ContainsId(object id) => ContainsId((TId)id);

        /// <inheritdoc/>
        bool IReferenceDataCollection.TryGetById(object id, [NotNullWhen(true)] out IReferenceData? item)
        {
            if (TryGetById((TId)id, out TRef? item2))
            {
                item = item2;
                return true;
            }

            item = null;
            return false;
        }

        /// <inheritdoc/>
        bool IReferenceDataCollection.TryGetByCode(string code, [NotNullWhen(true)] out IReferenceData? item)
        {
            if (TryGetByCode(code, out TRef? item2))
            {
                item = item2;
                return true;
            }

            item = null;
            return false;
        }

        /// <inheritdoc/>
        IReferenceData? IReferenceDataCollection.GetById(object id) => GetById((TId)id);

        /// <inheritdoc/>
        IReferenceData? IReferenceDataCollection.GetByCode(string code) => GetByCode(code);

        /// <inheritdoc/>
        IReferenceData? IReferenceDataCollection.GetByMapping<T>(string name, T value) => GetByMapping(name, value);

        /// <summary>
        /// Adds the <typeparamref name="TRef"/> <paramref name="item"/> to the <see cref="IReferenceDataCollection"/>.
        /// </summary>
        /// <param name="item">The <typeparamref name="TRef"/> item.</param>
        public void Add(TRef item);

        /// <summary>
        /// Adds the <paramref name="collection"/> to the <see cref="ReferenceDataCollection{TId, TRef}"/>.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        public void AddRange(IEnumerable<TRef> collection);

        /// <summary>
        /// Determines whether the specified <see cref="IIdentifier.Id"/> exists within the collection.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifier.Id"/>.</param>
        /// <returns><c>true</c> if it exists; otherwise, <c>false</c>.</returns>
        bool ContainsId(TId id);

        /// <summary>
        /// Attempts to get the <paramref name="item"/> with the specifed <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifier.Id"/>.</param>
        /// <param name="item">The corresponding <typeparamref name="TRef"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetById(TId id, [NotNullWhen(true)] out TRef? item);

        /// <summary>
        /// Attempts to get the <paramref name="item"/> with the specifed <paramref name="code"/>.
        /// </summary>
        /// <param name="code">The <see cref="IReferenceData.Code"/>.</param>
        /// <param name="item">The corresponding <typeparamref name="TRef"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetByCode(string code, [NotNullWhen(true)] out TRef? item);

        /// <summary>
        /// Gets the <typeparamref name="TRef"/> for the specified <see cref="IIdentifier.Id"/>.
        /// </summary>
        /// <param name="id">The specified reference data <see cref="IIdentifier.Id"/>.</param>
        /// <returns>The <typeparamref name="TRef"/> where found; otherwise, <c>null</c>.</returns>
        TRef? GetById(TId id);

        /// <summary>
        /// Gets the <typeparamref name="TRef"/> for the specified <see cref="IReferenceData.Code"/>.
        /// </summary>
        /// <param name="code">The specified <see cref="IReferenceData.Code"/>.</param>
        /// <returns>The <typeparamref name="TRef"/> where found; otherwise, <c>null</c>.</returns>
        new TRef? GetByCode(string code);

        /// <summary>
        /// Attempts to get the <paramref name="item"/> with the specifed <see cref="IReferenceData.GetMapping{T}(string)"/> value.
        /// </summary>
        /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <param name="item">The corresponding <see cref="IReferenceData"/> item where found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetByMapping<T>(string name, T value, [NotNullWhen(true)] out TRef? item) where T : IComparable<T>, IEquatable<T>;

        /// <summary>
        /// Gets the <see cref="IReferenceData"/> for the specified <see cref="IReferenceData.GetMapping{T}(string)"/> value.
        /// </summary>
        /// <typeparam name="T">The mapping value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <returns>The <see cref="IReferenceData"/> where found; otherwise, <c>null</c>.</returns>
        new TRef? GetByMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T>;
    }
}