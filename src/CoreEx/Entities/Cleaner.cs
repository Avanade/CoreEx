// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides capabilities to clean a specified value.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public static class Cleaner
    {
        private static DateTimeTransform _dateTimeTransform = DateTimeTransform.DateTimeUtc;
        private static StringTransform _stringTransform = StringTransform.EmptyToNull;
        private static StringTrim _stringTrim = StringTrim.End;

        /// <summary>
        /// Gets or sets the default <see cref="Entities.DateTimeTransform"/> for all entities unless explicitly overridden. Defaults to <see cref="DateTimeTransform.DateTimeUtc"/>.
        /// </summary>
        public static DateTimeTransform DefaultDateTimeTransform
        {
            get => _dateTimeTransform;
            set => _dateTimeTransform = value == DateTimeTransform.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultDateTimeTransform)) : value;
        }

        /// <summary>
        /// Gets or sets the default <see cref="Entities.DateTimeTransform"/> for all entities unless explicitly overridden. Defaults to <see cref="StringTransform.EmptyToNull"/>.
        /// </summary>
        public static StringTransform DefaultStringTransform
        {
            get => _stringTransform;
            set => _stringTransform = value == StringTransform.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringTransform)) : value;
        }

        /// <summary>
        /// Gets or sets the default <see cref="Entities.DateTimeTransform"/> for all entities unless explicitly overridden. Defaults to <see cref="StringTransform.EmptyToNull"/>.
        /// </summary>
        public static StringTrim DefaultStringTrim
        {
            get => _stringTrim;
            set => _stringTrim = value == StringTrim.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringTrim)) : value;
        }

        /// <summary>
        /// Cleans a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <returns>The cleaned value.</returns>
        /// <remarks>The <paramref name="value"/> will be trimmed and transformed using the respective <see cref="DefaultStringTrim"/> and <see cref="DefaultStringTransform"/> values.</remarks>
        public static string? Clean(string? value) => Clean(value, StringTrim.UseDefault, StringTransform.UseDefault);

        /// <summary>
        /// Cleans a <see cref="string"/> using the specified <paramref name="trim"/> and <paramref name="transform"/>.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <param name="trim">The <see cref="StringTrim"/> (defaults to <see cref="DefaultStringTrim"/>).</param>
        /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="DefaultStringTransform"/>).</param>
        /// <returns>The cleaned value.</returns>
        public static string? Clean(string? value, StringTrim trim = StringTrim.UseDefault, StringTransform transform = StringTransform.UseDefault)
        {
            if (trim == StringTrim.UseDefault)
                trim = DefaultStringTrim;

            if (transform == StringTransform.UseDefault)
                transform = DefaultStringTransform;

            // Handle a null string.
            if (value == null)
            {
                if (transform == StringTransform.NullToEmpty)
                    return string.Empty;
                else
                    return value;
            }

            // Trim the string.
            var tmp = trim switch
            {
                StringTrim.Both => value.Trim(),
                StringTrim.Start => value.TrimStart(),
                StringTrim.End => value.TrimEnd(),
                _ => value,
            };

            // Transform the string.
            return transform switch
            {
                StringTransform.EmptyToNull => (tmp.Length == 0) ? null : tmp,
                StringTransform.NullToEmpty => tmp ?? string.Empty,
                _ => tmp,
            };
        }

        /// <summary>
        /// Cleans a <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <returns>The cleaned value.</returns>
        /// <remarks>The <paramref name="value"/> will be transformed using <see cref="DateTimeTransform.UseDefault"/>.</remarks>
        public static DateTime Clean(DateTime value) => Clean(value, DateTimeTransform.UseDefault);

        /// <summary>
        /// Cleans a <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied.</param>
        /// <returns>The cleaned value.</returns>
        public static DateTime Clean(DateTime value, DateTimeTransform transform)
        {
            if (transform == DateTimeTransform.UseDefault)
                transform = DefaultDateTimeTransform;

            switch (transform)
            {
                case DateTimeTransform.DateOnly:
                    if (value.Kind == DateTimeKind.Unspecified && value.TimeOfDay == TimeSpan.Zero)
                        return value;
                    else
                        return DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

                case DateTimeTransform.DateTimeLocal:
                    if (value.Kind != DateTimeKind.Local)
                    {
                        if (value == DateTime.MinValue || value == DateTime.MaxValue || value.Kind == DateTimeKind.Unspecified)
                            return DateTime.SpecifyKind(value, DateTimeKind.Local);
                        else
                            return (value.Kind == DateTimeKind.Local) ? value : TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Local);
                    }

                    break;

                case DateTimeTransform.DateTimeUtc:
                    if (value.Kind != DateTimeKind.Utc)
                    {
                        if (value == DateTime.MinValue || value == DateTime.MaxValue || value.Kind == DateTimeKind.Unspecified)
                            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
                        else
                            return (value.Kind == DateTimeKind.Utc) ? value : TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Utc);
                    }

                    break;

                case DateTimeTransform.DateTimeUnspecified:
                    if (value.Kind != DateTimeKind.Unspecified)
                        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

                    break;
            }

            return value;
        }

        /// <summary>
        /// Cleans a <see cref="DateTime"/> value.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <returns>The cleaned value.</returns>
        /// <remarks>The <paramref name="value"/> will be transformed using <see cref="DateTimeTransform.UseDefault"/>.</remarks>
        public static DateTime? Clean(DateTime? value) => Clean(value, DateTimeTransform.UseDefault);

        /// <summary>
        /// Cleans a <see cref="Nullable{DateTime}"/> value.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <param name="transform">The <see cref="DateTimeTransform"/> to be applied.</param>
        /// <returns>The cleaned value.</returns>
        public static DateTime? Clean(DateTime? value, DateTimeTransform transform)
        {
            if (value == null || !value.HasValue)
                return value;

            return Clean(value.Value, transform);
        }

        /// <summary>
        /// Cleans a value and overrides the value with <c>null</c> when the value is <see cref="IInitial.IsInitial"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to clean.</param>
        /// <returns>The cleaned value.</returns>
        /// <remarks>This invokes <see cref="Clean{T}(T, bool)"/> with '<c>overrideWithNullWhenIsInitial</c>' parameter set to <c>true</c>.</remarks>
        public static T Clean<T>(T value) => Clean(value, true);

        /// <summary>
        /// Cleans a value.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to clean.</param>
        /// <param name="overrideWithNullWhenIsInitial">Indicates whether to override the value with <c>null</c> when the value is <see cref="IInitial.IsInitial"/>.</param>
        /// <returns>The cleaned value.</returns>
        public static T Clean<T>(T value, bool overrideWithNullWhenIsInitial)
        {
            if (value is string str)
                return (T)Convert.ChangeType(Clean(str, StringTrim.UseDefault, StringTransform.UseDefault), typeof(string), CultureInfo.CurrentCulture)!;
            else if (value is DateTime dte)
                return (T)Convert.ChangeType(Clean(dte, DateTimeTransform.UseDefault), typeof(DateTime), CultureInfo.CurrentCulture);

            if (value is ICleanUp ic)
                ic.CleanUp();

            if (overrideWithNullWhenIsInitial && value is IInitial ii && ii.IsInitial)
                return default!;

            return value;
        }

        /// <summary>
        /// Cleans one or more values where they implement <see cref="ICleanUp"/>.
        /// </summary>
        /// <param name="values">The values to clean.</param>
        public static void CleanUp(params object?[] values)
        {
            if (values != null)
            {
                foreach (object? o in values)
                {
                    if (o != null && o is ICleanUp value)
                        value.CleanUp();
                }
            }
        }

        /// <summary>
        /// Indicates whether a value is considered in its default state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> indicates that the value is initial; otherwise, <c>false</c>.</returns>
        /// <remarks>This determines whether is initial by comparing against its default value; this does not leverage <see cref="IInitial.IsInitial"/>.</remarks>
        public static bool IsDefault<T>(T value) => value == null || Comparer<T>.Default.Compare(value, default!) == 0;
    }
}