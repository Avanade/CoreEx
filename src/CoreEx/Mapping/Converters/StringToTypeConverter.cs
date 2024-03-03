// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.ComponentModel;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents a <see cref="string"/> to <typeparamref name="T"/> to conversion using a <see cref="TypeConverter"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to convert.</typeparam>
    /// <remarks>See also <see cref="TypeToStringConverter{T}"/>.</remarks>
    public readonly struct StringToTypeConverter<T> : IConverter<string?, T>
    {
        private static readonly TypeConverter _typeConverter = TypeDescriptor.GetConverter(typeof(T));
        private static readonly ValueConverter<string?, T> _convertToDestination = new(d => d == null ? default! : (T)_typeConverter.ConvertFromInvariantString(d)!);
        private static readonly ValueConverter<T, string?> _convertToSource = new(s => s == null ? default! : _typeConverter.ConvertToInvariantString(s));

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static StringToTypeConverter<T> Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringToTypeConverter{T}"/> struct.
        /// </summary>
        public StringToTypeConverter() { }

        /// <inheritdoc/>
        public readonly IValueConverter<string?, T> ToDestination => _convertToDestination;

        /// <inheritdoc/>
        public readonly IValueConverter<T, string?> ToSource => _convertToSource;

        /// <inheritdoc />
        public readonly object? ConvertToDestination(object? source) => ConvertToDestination((string?)source);

        /// <inheritdoc />
        public readonly object? ConvertToSource(object? destination) => ConvertToSource((T)destination!);

        /// <inheritdoc />
        public readonly T ConvertToDestination(string? source) => ToDestination.Convert(source);

        /// <inheritdoc />
        public readonly string? ConvertToSource(T destination) => ToSource.Convert(destination);
    }
}