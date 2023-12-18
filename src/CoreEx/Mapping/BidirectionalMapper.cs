// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Mapping;

/// <summary>
/// Provides a bidirectional mapper that encapsulates two <see cref="IMapper{TFrom, TTo}"/>; one for each direction.
/// </summary>
/// <typeparam name="TFrom">The from <see cref="Type"/>.</typeparam>
/// <typeparam name="TTo">The to <typeparamref name="TTo"/>.</typeparam>
/// <param name="mapperFromTo">The from/to mapper.</param>
/// <param name="mapperToFrom">The to/from mapper.</param>
public class BidirectionalMapper<TFrom, TTo>(IMapper<TFrom, TTo> mapperFromTo, IMapper<TTo, TFrom> mapperToFrom) : IBidirectionalMapper<TFrom, TTo> where TFrom : class, new() where TTo : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BidirectionalMapper{TFrom, TTo}"/> class.
    /// </summary>
    /// <param name="mapFromTo">The from/to mapping.</param>
    /// <param name="mapToFrom">The to/from mapping.</param>
    public BidirectionalMapper(Func<TFrom?, TTo?, OperationTypes, TTo?> mapFromTo, Func<TTo?, TFrom?, OperationTypes, TFrom?> mapToFrom) : this(new Mapper<TFrom, TTo>(mapFromTo), new Mapper<TTo, TFrom>(mapToFrom)) { }

    /// <inheritdoc/>
    public IMapper<TFrom, TTo> MapperFromTo { get; } = mapperFromTo.ThrowIfNull(nameof(mapperFromTo));

    /// <inheritdoc/>
    public IMapper<TTo, TFrom> MapperToFrom { get; } = mapperToFrom.ThrowIfNull(nameof(mapperToFrom));

    /// <summary>
    /// Maps the <paramref name="from"/> value to a new <typeparamref name="TTo"/> value.
    /// </summary>
    /// <param name="from">The source value.</param>
    /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
    /// <returns>The destination value.</returns>
    [return: NotNullIfNotNull(nameof(from))]
    public TTo? Map(TFrom? from, OperationTypes operationType = OperationTypes.Unspecified) => MapperFromTo.Map(from, operationType);

    /// <summary>
    /// Maps the <paramref name="source"/> value into the existing <paramref name="destination"/> value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
    /// <returns>The <paramref name="destination"/> value.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public TTo? Map(TFrom? source, TTo? destination, OperationTypes operationType = OperationTypes.Unspecified) => MapperFromTo.Map(source, destination, operationType);

    /// <summary>
    /// Maps the <paramref name="source"/> value to a new <typeparamref name="TTo"/> value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
    /// <returns>The destination value.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public TFrom? Map(TTo? source, OperationTypes operationType = OperationTypes.Unspecified) => MapperToFrom.Map(source, operationType);

    /// <summary>
    /// Maps the <paramref name="source"/> value into the existing <paramref name="destination"/> value.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
    /// <returns>The <paramref name="destination"/> value.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public TFrom? Map(TTo? source, TFrom? destination, OperationTypes operationType = OperationTypes.Unspecified) => MapperToFrom.Map(source, destination, operationType);
}