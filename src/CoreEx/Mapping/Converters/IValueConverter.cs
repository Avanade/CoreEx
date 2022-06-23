// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Enables conversion from a source to a destination value.
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Convert <paramref name="source"/> value to destination equivalent.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <returns>The destination equivalent.</returns>
        object? Convert(object? source);
    }
}