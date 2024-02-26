// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents an encoded <see cref="string"/> to <see cref="uint"/> converter.
    /// </summary>
    public readonly struct EncodedStringToUInt32Converter : IConverter<string?, uint>
    {
        private static readonly ValueConverter<string?, uint> _convertToDestination = new(s => s == null ? 0 : BitConverter.ToUInt32(Convert.FromBase64String(s)));
        private static readonly ValueConverter<uint, string?> _convertToSource = new(d => d == 0 ? null : Convert.ToBase64String(BitConverter.GetBytes(d)));

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static EncodedStringToUInt32Converter Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringToBase64Converter"/> struct.
        /// </summary>
        public EncodedStringToUInt32Converter() { }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<string?, uint> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<uint, string?> ToSource => _convertToSource;
    }
}