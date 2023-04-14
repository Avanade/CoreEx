// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx
{
    /// <summary>
    /// Provides standard <see cref="object"/> extensions.
    /// </summary>
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
        public static T Adjust<T>(this T value, Action<T> adjuster) where T : class
        {
            if (value is not null)
                adjuster?.Invoke(value ?? throw new ArgumentNullException(nameof(value)));

            return value!;
        }

        /// <summary>
        /// Converts a <see cref="string"/> into sentence case.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The <see cref="string"/> as sentence case.</returns>
        /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var Name DB</c>'.
        /// <para>Uses the <see cref="PropertyExpression.SentenceCaseConverter"/> function to perform the conversion.</para></remarks>
        [return: NotNullIfNotNull(nameof(text))]
        public static string? ToSentenceCase(this string? text) => PropertyExpression.ToSentenceCase(text);
    }
}