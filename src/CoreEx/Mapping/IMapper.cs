// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Provides mapping between source and destination values.
    /// </summary>
    /// <remarks>Decouples <i>CoreEx</i> from any specific implementation.</remarks>
    public interface IMapper
    {
        /// <summary>
        /// Maps the <paramref name="source"/> (inferring <see cref="Type"/>) value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        [return: NotNullIfNotNull(nameof(source))]
        TDestination? Map<TDestination>(object? source, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps the <paramref name="source"/> value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        [return: NotNullIfNotNull(nameof(source))]
        TDestination? Map<TSource, TDestination>(TSource? source, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps the <paramref name="source"/> value into the existing <paramref name="destination"/> value.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="destination">The destination value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The <paramref name="destination"/> value.</returns>
        [return: NotNullIfNotNull(nameof(source))]
        TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination, OperationTypes operationType = OperationTypes.Unspecified);
    }
}