﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents a <see cref="string"/> to <see cref="byte"/> <see cref="Array"/> converter (uses <see cref="Convert.FromBase64String(string)"/> and <see cref="Convert.ToBase64String(byte[])"/>).
    /// </summary>
    public readonly struct StringToBase64Converter : IConverter<string?, byte[]?>
    {
        private static readonly ValueConverter<string?, byte[]?> _convertToDestination = new(s => s == null ? null : Convert.FromBase64String(s));
        private static readonly ValueConverter<byte[]?, string?> _convertToSource = new(d => d == null ? null : Convert.ToBase64String(d));

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static StringToBase64Converter Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringToBase64Converter"/> struct.
        /// </summary>
        public StringToBase64Converter() { }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<string?, byte[]?> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<byte[]?, string?> ToSource => _convertToSource;

        /// <inheritdoc />
        public readonly object? ConvertToDestination(object? source) => ConvertToDestination((string?)source);

        /// <inheritdoc />
        public readonly object? ConvertToSource(object? destination) => ConvertToSource((byte[]?)destination);

        /// <inheritdoc />
        public readonly byte[]? ConvertToDestination(string? source) => ToDestination.Convert(source);

        /// <inheritdoc />
        public readonly string? ConvertToSource(byte[]? destination) => ToSource.Convert(destination);
    }
}