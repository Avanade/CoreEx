﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Text;
using System;
using System.Diagnostics.CodeAnalysis;
#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace CoreEx
{
    /// <summary>
    /// Provides standard <see cref="object"/> extensions.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public static class ObjectExtensions
    {
        /// <summary>
        /// Enables adjustment (changes) to a <paramref name="value"/> via an <paramref name="adjuster"/> action.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to adjust.</param>
        /// <param name="adjuster">The adjusting action (invoked only where the <paramref name="value"/> is not <c>null</c>).</param>
        /// <returns>The adjusted value (same instance).</returns>
        /// <remarks>Useful in scenarios to in-line simple changes to a value to simplify code.</remarks>
        [return: NotNullIfNotNull(nameof(value))]
        public static T? Adjust<T>(this T? value, Action<T> adjuster)
        {
            if (value is not null)
                adjuster?.Invoke(value);

            return value!;
        }

        /// <summary>
        /// Enables adjustment (changes) to a <paramref name="value"/> via an <paramref name="adjuster"/> action when the <paramref name="predicate"/> is <c>true</c>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to adjust.</param>
        /// <param name="predicate">The <see cref="Predicate{T}"/> that determines whether the <paramref name="predicate"/> is invoked.</param>
        /// <param name="adjuster">The adjusting action (invoked only where the <paramref name="value"/> is not <c>null</c> and the <paramref name="predicate"/> results in <c>true</c>).</param>
        /// <returns>The adjusted value (same instance).</returns>
        /// <remarks>Useful in scenarios to in-line simple changes to a value to simplify code.</remarks>
        [return: NotNullIfNotNull(nameof(value))]
        public static T? AdjustWhen<T>(this T? value, Predicate<T> predicate, Action<T> adjuster)
        {
            if (value is not null && predicate(value))
                adjuster?.Invoke(value);

            return value!;
        }

        /// <summary>
        /// Converts a <see cref="string"/> into sentence case.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The <see cref="string"/> as sentence case.</returns>
        /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var Name DB</c>'.
        /// <para>Uses the <see cref="SentenceCase.ToSentenceCase(string?)"/> function to perform the conversion.</para></remarks>
        [return: NotNullIfNotNull(nameof(text))]
        public static string? ToSentenceCase(this string? text) => SentenceCase.ToSentenceCase(text);

#if NET6_0_OR_GREATER
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
        /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
        public static T ThrowIfNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(value, paramName);
            return value;
        }
#else
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
        /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
        public static T ThrowIfNull<T>([NotNull] this T? value, string? paramName = "value")
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
            
            return value;
        }
#endif

#if NET7_0_OR_GREATER
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </summary>
        /// <param name="value">The value to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
        /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
        public static string ThrowIfNullOrEmpty([NotNull] this string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(value, paramName);
            return value;
        }
#else
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </summary>
        /// <param name="value">The value to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
        /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
        public static string ThrowIfNullOrEmpty([NotNull] this string? value, string? paramName = "value")
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(paramName);
            
            return value;
        }
#endif

#if NET7_0_OR_GREATER
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </summary>
        /// <param name="value">The value to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
        /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
        public static string? ThrowIfEmpty(this string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value is not null && 0 == value.Length)
                throw new ArgumentException("The value cannot be an empty string.", paramName);

            return value;
        }
#else
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the <paramref name="value"/> is <c>null</c> or <see cref="string.Empty"/>.
        /// </summary>
        /// <param name="value">The value to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which the <paramref name="value"/> corresponds.</param>
        /// <returns>The <paramref name="value"/> to support fluent-style method-chaining.</returns>
        public static string? ThrowIfEmpty(this string? value, string? paramName = "value")
        {
            if (value is not null && 0 == value.Length)
                throw new ArgumentException("The value cannot be an empty string.", paramName);

            return value;
        }
#endif
    }
}