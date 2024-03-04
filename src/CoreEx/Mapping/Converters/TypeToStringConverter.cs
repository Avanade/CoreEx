// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.ComponentModel;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents a <typeparamref name="T"/> to <see cref="string"/> conversion using a <see cref="TypeConverter"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to convert.</typeparam>
    /// <remarks>See also <see cref="StringToTypeConverter{T}"/>.</remarks>
    public readonly struct TypeToStringConverter<T> : IConverter<T, string?>
    {
        private static readonly TypeConverter _typeConverter = TypeDescriptor.GetConverter(typeof(T));
        private static readonly ValueConverter<T, string?> _convertToDestination = new(s => s == null ? default! : _typeConverter.ConvertToInvariantString(s));
        private static readonly ValueConverter<string?, T> _convertToSource = new(d => d == null ? default! : (T)_typeConverter.ConvertFromInvariantString(d)!);

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static TypeToStringConverter<T> Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeToStringConverter{T}"/> struct.
        /// </summary>
        public TypeToStringConverter() { }

        /// <inheritdoc/>
        public readonly IValueConverter<T, string?> ToDestination => _convertToDestination;

        /// <inheritdoc/>
        public readonly IValueConverter<string?, T> ToSource => _convertToSource;

        /// <inheritdoc />
        public readonly object? ConvertToDestination(object? source) => ConvertToDestination((T)source!);

        /// <inheritdoc />
        public readonly object? ConvertToSource(object? destination) => ConvertToSource((string?)destination);

        /// <inheritdoc />
        public readonly string? ConvertToDestination(T source) => ToDestination.Convert(source);

        /// <inheritdoc />
        public readonly T ConvertToSource(string? destination) => ToSource.Convert(destination);
    }
}