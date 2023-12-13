// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Enables the <see cref="Create"/> of a <see cref="Converter{TSource, TDestination}"/> instance.
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// Provides a generic means to create a one-off <see cref="Converter{TSource, TDestination}"/> instance.
        /// </summary>
        /// <typeparam name="TSource">The source <see cref="Type"/>.</typeparam>
        /// <typeparam name="TDestination">The destination <see cref="Type"/>.</typeparam>
        /// <param name="toDestination">The <typeparamref name="TSource"/> to <typeparamref name="TDestination"/> conversion logic.</param>
        /// <param name="toSource">The <typeparamref name="TDestination"/> to <typeparamref name="TSource"/> conversion logic.</param>
        /// <returns>The <see cref="Converter{TSource, TDestination}"/>.</returns>
        public static Converter<TSource, TDestination> Create<TSource, TDestination>(Func<TSource, TDestination> toDestination, Func<TDestination, TSource> toSource) => new(toDestination, toSource);
    }
}