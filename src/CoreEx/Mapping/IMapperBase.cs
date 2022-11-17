// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Enables the base source and destination mapping capability.
    /// </summary>
    public interface IMapperBase
    {
        /// <summary>
        /// Gets or sets the owning <see cref="Mapper"/>.
        /// </summary>
        /// <remarks>This enables a mapper to map an underlying property to another.
        /// <para>This is automatically set during the <see cref="Mapper.Register{TSource, TDestination}(IMapper{TSource, TDestination})"/>.</para></remarks>
        Mapper Owner { get; set; }

        /// <summary>
        /// Gets the source <see cref="Type"/>.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets the destination <see cref="Type"/>.
        /// </summary>
        Type DestinationType { get; }

        /// <summary>
        /// Maps the <paramref name="source"/> value to a new destination value.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        object? Map(object? source, OperationTypes operationType = OperationTypes.Unspecified);

        /// <summary>
        /// Maps the <paramref name="source"/> value to a new destination value.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="destination">The destination value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The destination value.</returns>
        object? Map(object? source, object? destination, OperationTypes operationType = OperationTypes.Unspecified);
    }
}