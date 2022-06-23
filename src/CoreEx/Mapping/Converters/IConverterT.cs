// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Enables bi-directional conversion from a source to a destination value and vice-versa.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public interface IConverter<TSource, TDestination> : IConverter
    {
        /// <inheritdoc/>
        Type IConverter.SourceType => typeof(TSource);

        /// <inheritdoc/>
        Type IConverter.DestinationType => typeof(TDestination);

        /// <inheritdoc/>
        object? IConverter.ConvertToDestination(object? source) => ToDestination.Convert((TSource?)source);

        /// <inheritdoc/>
        object? IConverter.ConvertToSource(object? destination) => ToSource.Convert((TDestination?)destination);

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        IValueConverter<TSource, TDestination> ToDestination { get; }

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        IValueConverter<TDestination, TSource> ToSource { get; }
    }
}