// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Represents the <see cref="Mapper{TSource, TDestination}"/> options.
    /// </summary>
    public class MapperOptions
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="MapperOptions"/> class.
        /// </summary>
        /// <param name="mapper">The owning <see cref="Owner"/>.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        public MapperOptions(Mapper mapper, OperationTypes operationType)
        {
            Owner = mapper ?? throw new ArgumentNullException(nameof(mapper));
            OperationType = operationType;
        }

        /// <summary>
        /// Gets the owning <see cref="Mapper"/>.
        /// </summary>
        public Mapper Owner { get; }

        /// <summary>
        /// Gets the singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.
        /// </summary>
        public OperationTypes OperationType { get; }

        /// <summary>
        /// Maps the <paramref name="source"/> (inferring <see cref="Type"/>) value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="source">The source value.</param>
        /// <returns>The destination value.</returns>
        public TDestination? Map<TDestination>(object? source)
            => Owner.Map<TDestination>(source, OperationType);

        /// <summary>
        /// Maps the <paramref name="source"/> value to a new <typeparamref name="TDestination"/> value.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="source">The source value.</param>
        /// <returns>The destination value.</returns>
        public TDestination? Map<TSource, TDestination>(TSource? source)
            => Owner.Map<TSource, TDestination>(source, OperationType);

        /// <summary>
        /// Maps the <paramref name="source"/> value into the existing <paramref name="destination"/> value.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="source">The source value.</param>
        /// <param name="destination">The destination value.</param>
        /// <returns>The <paramref name="destination"/> value.</returns>
        public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination)
            => Owner.Map(source, destination, OperationType);
    }
}