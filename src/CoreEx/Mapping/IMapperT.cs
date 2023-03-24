// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Enables mapping from the <typeparamref name="TSource"/> to the <typeparamref name="TDestination"/>.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public interface IMapper<TSource, TDestination> : IMapperBase
    {
        /// <inheritdoc/>
        Type IMapperBase.SourceType => typeof(TSource);

        /// <inheritdoc/>
        Type IMapperBase.DestinationType => typeof(TDestination);

        /// <summary>
        /// Creates a <typeparamref name="TSource"/> instance.
        /// </summary>
        /// <returns>A <typeparamref name="TSource"/> instance.</returns>
        TDestination CreateSource();

        /// <summary>
        /// Creates a <typeparamref name="TDestination"/> instance.
        /// </summary>
        /// <returns>A <typeparamref name="TDestination"/> instance.</returns>
        TDestination CreateDestination();

        /// <summary>
        /// Indicates whether the <paramref name="source"/> is considered initial; i.e. all mapped property values are their default.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <returns><c>true</c> where considered initial; otherwise, <c>false</c>.</returns>
        bool IsSourceInitial(TSource source);

        /// <summary>
        /// Initializes the destination properties to their default values during a <i>Flatten</i> where the source value is <c>null</c>.
        /// </summary>
        /// <param name="destination">The destination value.</param>
        /// <returns><c>true</c> where initialization occured; otherwise, <c>false</c>.</returns>
        bool InitializeDestination(TDestination destination);

        /// <summary>
        /// Maps the <paramref name="source"/> value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        TDestination? Map(TSource? source, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps the <paramref name="source"/> value into the existing <paramref name="destination"/> value.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="destination">The destination value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The <paramref name="destination"/> value.</returns>
        TDestination? Map(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified);
    }
}