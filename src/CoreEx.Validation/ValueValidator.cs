// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables validation for a value.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="name">The value name (defaults to <see cref="Validation.ValueNameDefault"/>).</param>
    /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
    public class ValueValidator<T>(T value, string? name = null, LText? text = null) : PropertyRuleBase<ValidationValue<T>, T>(string.IsNullOrEmpty(name) ? Validation.ValueNameDefault : name, text)
    {
        /// <summary>
        /// Gets the <see cref="ValidationValue{T}"/>.
        /// </summary>
        public ValidationValue<T> ValidationValue { get; } = new ValidationValue<T>(null, value);

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T? Value { get => ValidationValue.Value; }

        /// <inheritdoc/>
        public override Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(CancellationToken cancellationToken = default)
        {
            return ValidationInvoker.Current.InvokeAsync(this, async (_, cancellationToken) =>
            {
                var ctx = new PropertyContext<ValidationValue<T>, T>(new ValidationContext<ValidationValue<T>>(ValidationValue, new ValidationArgs()), Value, Name, JsonName, Text);
                await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);
                return new ValueValidatorResult<ValidationValue<T>, T>(ctx);
            }, cancellationToken);
        }
    }
}