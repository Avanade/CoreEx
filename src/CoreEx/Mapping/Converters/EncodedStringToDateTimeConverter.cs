// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents an encoded <see cref="string"/> to <see cref="DateTime"/> converter.
    /// </summary>
    public readonly struct EncodedStringToDateTimeConverter : IConverter<string?, DateTime?>
    {
        private static readonly ValueConverter<string?, DateTime?> _convertToDestination = new(s => s == null ? null : DateTime.FromBinary(BitConverter.ToInt64(Convert.FromBase64String(s))));
        private static readonly ValueConverter<DateTime?, string?> _convertToSource = new(d => d == null ? null : Convert.ToBase64String(BitConverter.GetBytes(d.Value.ToBinary())));

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static EncodedStringToDateTimeConverter Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringToBase64Converter"/> struct.
        /// </summary>
        public EncodedStringToDateTimeConverter() { }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<string?, DateTime?> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<DateTime?, string?> ToSource => _convertToSource;
    }
}