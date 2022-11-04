// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Provides conversion from a source to a destination value.
    /// </summary>
    /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
    public struct ValueConverter<TSource, TDestination> : IValueConverter<TSource, TDestination>
    {
        private readonly Func<TSource, TDestination> _converter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueConverter{TSource, TDestination}"/>.
        /// </summary>
        /// <param name="converter">The function to convert a <typeparamref name="TSource"/> to a <typeparamref name="TDestination"/>.</param>
        public ValueConverter(Func<TSource, TDestination> converter) => _converter = converter ?? throw new ArgumentNullException(nameof(converter));

        /// <inheritdoc/>
        public TDestination Convert(TSource source) => _converter(source);
    }
}