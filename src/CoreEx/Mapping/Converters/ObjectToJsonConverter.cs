// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents an <see cref="object"/> to JSON <see cref="string"/> converter.
    /// </summary>
    /// <typeparam name="T">The <see cref="object"/> <see cref="Type"/>.</typeparam>
    public struct ObjectToJsonConverter<T> : IConverter<T?, string?>
    {
        private readonly ValueConverter<T?, string?> _convertToDestination;
        private readonly ValueConverter<string?, T?> _convertToSource;

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static ObjectToJsonConverter<T> Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectToJsonConverter{T}"/> struct using the default <see cref="IJsonSerializer"/>.
        /// </summary>
        public ObjectToJsonConverter() : this(null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectToJsonConverter{T}"/> struct.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>; will default where not specified.</param>
        public ObjectToJsonConverter(IJsonSerializer? jsonSerializer)
        {
            var js = jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? JsonSerializer.Default;
            _convertToDestination = new(s => s == null ? null : js.Serialize(s));
            _convertToSource = new(d => d == null ? default : js.Deserialize<T>(d));
        }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<T?, string?> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<string?, T?> ToSource => _convertToSource;
    }
}