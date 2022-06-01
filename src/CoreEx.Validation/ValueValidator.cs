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
    public class ValueValidator<T> : PropertyRuleBase<ValidationValue<T>, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueValidator{T}"/> class.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name (defaults to <see cref="Validator.ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        public ValueValidator(T value, string? name = null, LText? text = null) : base(string.IsNullOrEmpty(name) ? Validator.ValueNameDefault : name, text) => ValidationValue = new ValidationValue<T>(null, value);

        /// <summary>
        /// Gets the <see cref="ValidationValue{T}"/>.
        /// </summary>
        public ValidationValue<T> ValidationValue { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T? Value { get => ValidationValue.Value; }

        /// <inheritdoc/>
        public override async Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(CancellationToken cancellationToken = default)
        {
            var ctx = new PropertyContext<ValidationValue<T>, T>(new ValidationContext<ValidationValue<T>>(null!, new ValidationArgs()), Value, this.Name, this.JsonName, this.Text);
            await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);
            return new ValueValidatorResult<ValidationValue<T>, T>(ctx);
        }
    }
}