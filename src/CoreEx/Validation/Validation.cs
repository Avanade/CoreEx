// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.Localization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Adds validation-related extension methods.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Gets or sets the format string for the Mandatory error message.
        /// </summary>
        /// <remarks>Defaults to: '<c>{0} is required.</c>'.</remarks>
        public static LText MandatoryFormat { get; set; } = new("CoreEx.Validation.MandatoryFormat", "{0} is required.");

        /// <summary>
        /// Gets or sets the default value name.
        /// </summary>
        /// <remarks>Defaults to: '<c>value</c>'.</remarks>
        public static string ValueNameDefault { get; set; } = "value";

        /// <summary>
        /// Gets or sets the default value <see cref="LText"/>.
        /// </summary>
        public static LText ValueTextDefault { get; set; } = "Value";

        /// <summary>
        /// Validates the value asynchronously using the specified <paramref name="validator"/>.
        /// </summary>
        /// <typeparam name="T">The underlying value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="validator">The corresponding <see cref="IValidator{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The validated value.</returns>
        /// <exception cref="ValidationException">Thrown where a validation error(s) occurs.</exception>
        public static async Task<T?> ValidateValueAsync<T>(this T value, IValidator<T> validator, CancellationToken cancellationToken = default) where T : class
        {
            (await (validator ?? throw new ArgumentNullException(nameof(validator))).ValidateAsync(value, cancellationToken).ConfigureAwait(false)).ThrowOnError();
            return value;
        }

        /// <summary>
        /// Ensures that the <paramref name="value"/> is non-default and continues; otherwise, will throw a <see cref="ValidationException"/>.
        /// </summary>
        /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name (defaults to <see cref="ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>The value where non-default.</returns>
        /// <exception cref="ValidationException">Thrown where the value is default.</exception>
        public static T? EnsureValue<T>(this T? value, string? name = null, LText? text = null) 
            => (Comparer<T?>.Default.Compare(value, default!) == 0) ? throw new ValidationException(MessageItem.CreateErrorMessage(name ?? ValueNameDefault, MandatoryFormat, text ?? ((name == null || name == ValueNameDefault) ? ValueTextDefault : name.ToSentenceCase()!))) : value;
    }
}