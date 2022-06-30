// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Enables bi-directional conversion from a source to a destination value and vice-versa.
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// Gets the source <see cref="Type"/>.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets the destination <see cref="Type"/>.
        /// </summary>
        Type DestinationType { get; }

        /// <summary>
        /// Converts the source to the destination value.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <returns>The destination value.</returns>
        object? ConvertToDestination(object? source);

        /// <summary>
        /// Converts the destination to the source value.
        /// </summary>
        /// <param name="destination">The destination value.</param>
        /// <returns>The source value.</returns>
        object? ConvertToSource(object? destination);
    }
}