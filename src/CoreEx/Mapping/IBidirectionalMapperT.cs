// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping
{
    /// <summary>
    /// Provides a bidirectional mapper that encapsulates two <see cref="IMapper{TSource, TDestination}"/>; one for each direction.
    /// </summary>
    /// <typeparam name="TFrom">The from <see cref="Type"/>.</typeparam>
    /// <typeparam name="TTo">The to <typeparamref name="TTo"/>.</typeparam>
    public interface IBidirectionalMapper<TFrom, TTo> : IBidirectionalMapperBase
    {
        /// <inheritdoc/>
        IMapperBase IBidirectionalMapperBase.MapperFromTo => MapperFromTo;

        /// <inheritdoc/>
        IMapperBase IBidirectionalMapperBase.MapperToFrom => MapperToFrom;

        /// <summary>
        /// Gets the <see cref="IMapper{TSource, TDestination}"/> for <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>
        /// </summary>
        new IMapper<TFrom, TTo> MapperFromTo { get; }

        /// <summary>
        /// Gets the <see cref="IMapper{TSource, TDestination}"/> for <typeparamref name="TTo"/> to <typeparamref name="TFrom"/>"/>
        /// </summary>
        new IMapper<TTo, TFrom> MapperToFrom { get; }
    }
}