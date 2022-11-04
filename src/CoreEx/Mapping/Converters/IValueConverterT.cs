// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Enables conversion from a source to a destination value.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public interface IValueConverter<TSource, TDestination> : IValueConverter
    {
        /// <inheritdoc/>
        object? IValueConverter.Convert(object? source) => Convert(source == null ? default! : (TSource)source!);

        /// <summary>
        /// Convert <paramref name="source"/> value to destination equivalent.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <returns>The destination equivalent.</returns>
        TDestination Convert(TSource source);
    }
}