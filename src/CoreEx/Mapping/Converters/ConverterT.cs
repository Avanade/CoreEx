// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Provides a generic means to create a one-off <see cref="IConverter{TSource, TDestination}"/> instance.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="toDestination">The <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> conversion logic.</param>
    /// <param name="toSource">The <typeparamref name="TDestination"/> to <typeparamref name="TSource"/> conversion logic.</param>
    public readonly struct Converter<TSource, TDestination>(Func<TSource, TDestination> toDestination, Func<TDestination, TSource> toSource) : IConverter<TSource, TDestination>
    {
        private readonly ValueConverter<TSource, TDestination> _convertToDestination = new(toDestination);
        private readonly ValueConverter<TDestination, TSource> _convertToSource = new(toSource);

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public readonly IValueConverter<TSource, TDestination> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public readonly IValueConverter<TDestination, TSource> ToSource => _convertToSource;
    }
}