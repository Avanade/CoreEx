// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Globalization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private static StringCase _stringCase = StringCase.None;

        /// <summary>
        /// Gets or sets the default <see cref="Entities.DateTimeTransform"/> for all entities unless explicitly overridden. Defaults to <see cref="DateTimeTransform.DateTimeUtc"/>.
        /// </summary>
        public static DateTimeTransform DefaultDateTimeTransform
        {
            get => _dateTimeTransform;
            set => _dateTimeTransform = value == DateTimeTransform.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultDateTimeTransform)) : value;
        }

        /// <summary>
        /// Gets or sets the default <see cref="Entities.StringTransform"/> for all entities unless explicitly overridden. Defaults to <see cref="StringTransform.EmptyToNull"/>.
        /// </summary>
        public static StringTransform DefaultStringTransform
        {
            get => _stringTransform;
            set => _stringTransform = value == StringTransform.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringTransform)) : value;
        }

        /// <summary>
        /// Gets or sets the default <see cref="Entities.StringTrim"/> for all entities unless explicitly overridden. Defaults to <see cref="StringTrim.End"/>.
        /// </summary>
        public static StringTrim DefaultStringTrim
        {
            get => _stringTrim;
            set => _stringTrim = value == StringTrim.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringTrim)) : value;
        }

        /// <summary>
        /// Gets or sets the default <see cref="Entities.StringCase"/> for all entities unless explicitly overridden. Defaults to <see cref="StringCase.None"/>.
        /// </summary>
        public static StringCase DefaultStringCase
        {
            get => _stringCase;
            set => _stringCase = value == StringCase.UseDefault ? throw new ArgumentException("The default cannot be set to UseDefault.", nameof(DefaultStringCase)) : value;
        }

        /// <summary>
        /// Cleans a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <returns>The cleaned value.</returns>
        /// <remarks>The <paramref name="value"/> will be trimmed and transformed using the respective <see cref="DefaultStringTrim"/> and <see cref="DefaultStringTransform"/> values.</remarks>
        public static string? Clean(string? value) => Clean(value, StringTrim.UseDefault, StringTransform.UseDefault, StringCase.UseDefault);

        /// <summary>
        /// Cleans a <see cref="string"/> using the specified <paramref name="trim"/> and <paramref name="transform"/>.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <param name="trim">The <see cref="StringTrim"/> (defaults to <see cref="DefaultStringTrim"/>).</param>
        /// <param name="transform">The <see cref="StringTransform"/> (defaults to <see cref="DefaultStringTransform"/>).</param>
        /// <param name="casing">The <see cref="StringCase"/> (defaults to <see cref="DefaultStringCase"/>).</param>
        /// <returns>The cleaned value.</returns>
        public static string? Clean(string? value, StringTrim trim = StringTrim.UseDefault, StringTransform transform = StringTransform.UseDefault, StringCase casing = StringCase.UseDefault)
        {
            if (transform == StringTransform.UseDefault)
                transform = ExecutionContext.GetService<SettingsBase>()?.StringTransform ?? DefaultStringTransform;

            // Handle a null string.
            if (value == null)
            {
                if (transform == StringTransform.NullToEmpty)
                    return string.Empty;
                else
                    return value;
            }

            if (trim == StringTrim.UseDefault)
                trim = ExecutionContext.GetService<SettingsBase>()?.StringTrim ?? DefaultStringTrim;

            // Trim the string.
            var tmp = trim switch
            {
                StringTrim.Both => value.Trim(),
                StringTrim.Start => value.TrimStart(),
                StringTrim.End => value.TrimEnd(),
                _ => value,
            };

            // Transform the string.
            tmp = transform switch
            {
                StringTransform.EmptyToNull => (tmp.Length == 0) ? null : tmp,
                StringTransform.NullToEmpty => tmp ?? string.Empty,
                _ => tmp,
            };

            if (string.IsNullOrEmpty(tmp))
                return tmp;

            // Apply casing to the string.
            if (casing == StringCase.UseDefault)
                casing = ExecutionContext.GetService<SettingsBase>()?.StringCase ?? DefaultStringCase;

            return casing switch
            {
                StringCase.Lower => CultureInfo.CurrentCulture.TextInfo.ToCasing(tmp, TextInfoCasing.Lower),
                StringCase.Upper => CultureInfo.CurrentCulture.TextInfo.ToCasing(tmp, TextInfoCasing.Upper),
                StringCase.Title => CultureInfo.CurrentCulture.TextInfo.ToCasing(tmp, TextInfoCasing.Title),
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
        /// <remarks>Will attempt to use <see cref="SettingsBase.DateTimeTransform"/> as a default where possible.</remarks>
        public static DateTime Clean(DateTime value, DateTimeTransform transform)
        {
            if (transform == DateTimeTransform.UseDefault)
                transform = ExecutionContext.GetService<SettingsBase>()?.DateTimeTransform ?? DefaultDateTimeTransform;

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
        /// <remarks>This invokes <see cref="Clean{T}(T, bool)"/> with '<c>overrideWithDefaultWhenIsInitial</c>' parameter set to <c>true</c>.</remarks>
        public static T Clean<T>(T value) => Clean(value, true);

        /// <summary>
        /// Cleans a value.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to clean.</param>
        /// <param name="overrideWithDefaultWhenIsInitial">Indicates whether to override the value with <c>default</c> when the value is <see cref="IInitial.IsInitial"/>.</param>
        /// <returns>The cleaned value.</returns>
        public static T Clean<T>(T value, bool overrideWithDefaultWhenIsInitial)
        {
            if (value is string str)
                return (T)Convert.ChangeType(Clean(str, StringTrim.UseDefault, StringTransform.UseDefault, StringCase.UseDefault), typeof(string), CultureInfo.CurrentCulture)!;
            else if (value is DateTime dte)
                return (T)Convert.ChangeType(Clean(dte, DateTimeTransform.UseDefault), typeof(DateTime), CultureInfo.CurrentCulture);

            if (value is ICleanUp ic)
                ic.CleanUp();

            if (overrideWithDefaultWhenIsInitial && value is IInitial ii && ii.IsInitial)
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

        /// <summary>
        /// Indicates whether a value is considered in its default state.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to check.</param>
        /// <param name="default">The default value override.</param>
        /// <returns><c>true</c> indicates that the value is initial; otherwise, <c>false</c>.</returns>
        /// <remarks>This determines whether is initial by comparing against its default value; this does not leverage <see cref="IInitial.IsInitial"/>.</remarks>
        public static bool IsDefault<T>(T value, T @default) => Comparer<T>.Default.Compare(value, @default) == 0;

        /// <summary>
        /// Resets the <see cref="ITenantId.TenantId"/> by overridding with <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.TenantId"/> where <see cref="ExecutionContext.HasCurrent"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        public static void ResetTenantId<T>(T? value, ExecutionContext? executionContext = null)
        {
            if (value == null || value is not ITenantId ti)
                return;

            if (executionContext is null)
            {

                if (ExecutionContext.HasCurrent)
                    ti.TenantId = ExecutionContext.Current.TenantId;
            }
            else
                ti.TenantId = executionContext.TenantId;
        }

        /// <summary>
        /// Prepares the value for create by encapsulating <see cref="ChangeLog.PrepareCreated{T}(T, ExecutionContext?)"/> and <see cref="ResetTenantId"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <returns>The value to support fluent-style method-chaining.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public static T? PrepareCreate<T>(T? value, ExecutionContext? executionContext = null)
        {
            if (value is not null)
            {
                ChangeLog.PrepareCreated(value, executionContext);
                ResetTenantId(value, executionContext);
            }

            return value;
        }

        /// <summary>
        /// Prepares the value for update by encapsulating <see cref="ChangeLog.PrepareUpdated{T}(T, ExecutionContext?)"/> and <see cref="ResetTenantId"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
        /// <returns>The value to support fluent-style method-chaining.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public static T? PrepareUpdate<T>(T? value, ExecutionContext? executionContext = null)
        {
            if (value is not null)
            {
                ChangeLog.PrepareUpdated(value, executionContext);
                ResetTenantId(value, executionContext);
            }

            return value;
        }
    }
}