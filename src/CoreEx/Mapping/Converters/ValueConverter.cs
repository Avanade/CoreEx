// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Provides conversion from a source to a destination value.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    /// <param name="converter">The function to convert a <typeparamref name="TSource"/> to a <typeparamref name="TDestination"/>.</param>
    public readonly struct ValueConverter<TSource, TDestination>(Func<TSource, TDestination> converter) : IValueConverter<TSource, TDestination>
    {
        private readonly Func<TSource, TDestination> _converter = converter.ThrowIfNull(nameof(converter));

        /// <inheritdoc/>
        public TDestination Convert(TSource source) => _converter is null ? throw new InvalidOperationException($"The {nameof(ValueConverter<TSource, TDestination>)} has not been initialized with an underlying converter correctly.") : _converter(source);
    }
}