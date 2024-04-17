// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables the ability to configure the <see cref="ValueValidator{T}"/>.
    /// </summary>
    public class ValueValidatorConfiguration<T> : PropertyRuleBase<ValidationValue<T>, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueValidatorConfiguration{T}"/> class.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as <see cref="Text.SentenceCase.ToSentenceCase(string)"/>).</param>
        /// <param name="jsonName">The JSON property name (defaults to <paramref name="name"/>).</param>
        internal ValueValidatorConfiguration(string name, LText? text = null, string? jsonName = null) : base(name, text, jsonName) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="validationValue">The <see cref="ValidationValue{T}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        internal Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(ValidationValue<T> validationValue, CancellationToken cancellationToken)
        {
            return ValidationInvoker.Current.InvokeAsync(this, async (_, cancellationToken) =>
            {
                var ctx = new PropertyContext<ValidationValue<T>, T>(new ValidationContext<ValidationValue<T>>(validationValue, new ValidationArgs()), validationValue.Value, Name, JsonName, Text);
                await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);
                return new ValueValidatorResult<ValidationValue<T>, T>(ctx);
            }, cancellationToken);
        }
    }
}