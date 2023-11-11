// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Globalization;

namespace CoreEx.Mapping.Converters
{
    /// <summary>
    /// Represents a <see cref="DateTime"/> to <see cref="string"/> converter.
    /// </summary>
    /// <remarks>The default format is '<c>G</c>' (see <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#the-general-date-long-time-g-format-specifier"/>)
    /// with an <see cref="IFormatProvider"/> of <see cref="CultureInfo.InvariantCulture"/> <see cref="CultureInfo.DateTimeFormat"/>.</remarks>
    public readonly struct DateTimeToStringConverter : IConverter<DateTime?, string?>
    {
        private readonly ValueConverter<DateTime?, string?> _convertToDestination = new(s => s?.ToString("G", CultureInfo.InvariantCulture.DateTimeFormat));
        private readonly ValueConverter<string?, DateTime?> _convertToSource = new(d => d == null ? null : DateTime.ParseExact(d, "G", CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None));

        /// <summary>
        /// Gets or sets the default (singleton) instance.
        /// </summary>
        public static DateTimeToStringConverter Default { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringToBase64Converter"/> struct.
        /// </summary>
        public DateTimeToStringConverter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringToBase64Converter"/> struct.
        /// </summary>
        /// <param name="format">The <see cref="DateTime"/> format specifier.</param>
        /// <param name="formatProvider">The <see cref="IFormatProvider"/>; defaults to <see cref="CultureInfo.InvariantCulture"/> <see cref="CultureInfo.DateTimeFormat"/>.</param>
        /// <param name="style">The <see cref="DateTimeStyles"/>; defaults to <see cref="DateTimeStyles.None"/>.</param>
        /// <param name="transform">The optional <see cref="DateTimeTransform"/> to be performed on the <see cref="DateTime"/>.</param>
        public DateTimeToStringConverter(string format, IFormatProvider? formatProvider = null, DateTimeStyles style = DateTimeStyles.None, DateTimeTransform transform = DateTimeTransform.None)
        {
            format = format ?? throw new ArgumentNullException(nameof(format));
            formatProvider ??= CultureInfo.InvariantCulture.DateTimeFormat;
            _convertToDestination = new(s => Cleaner.Clean(s, transform)?.ToString(format, formatProvider));
            _convertToSource = new(d => d == null ? null : Cleaner.Clean(DateTime.ParseExact(d, format, formatProvider, style), transform));
        }

        /// <summary>
        /// Gets the source to destination <see cref="IValueConverter{TSource, TDestination}"/>.
        /// </summary>
        public IValueConverter<DateTime?, string?> ToDestination => _convertToDestination;

        /// <summary>
        /// Gets the destination to source <see cref="IValueConverter{TDestination, TSource}"/>.
        /// </summary>
        public IValueConverter<string?, DateTime?> ToSource => _convertToSource;
    }
}