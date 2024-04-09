// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables a validation rule for an entity property. 
    /// </summary>
    public interface IPropertyRule
    {
        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        public string JsonName { get; }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        public LText Text { get; set; }

        /// <summary>
        /// Gets or sets the error message format text (overrides the default) used for all validation errors.
        /// </summary>
        public LText? ErrorText { get; set; }

        /// <summary>
        /// Executes the validation for the property value.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        /// <remarks>This may not be supported by all implementations; in which case a <see cref="NotSupportedException"/> may be thrown.</remarks>
        Task<IValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the validation for the property value.
        /// </summary>
        /// <param name="throwOnError">Indicates whether to automatically throw a <see cref="ValidationException"/> where <see cref="IValidationResult.HasErrors"/>.</param>
        /// <param name="cancellationToken">>The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        /// <remarks>This may not be supported by all implementations; in which case a <see cref="NotSupportedException"/> may be thrown.</remarks>
        public async Task<IValidationResult> ValidateAsync(bool throwOnError, CancellationToken cancellationToken = default)
        {
            var ir = await ValidateAsync(cancellationToken).ConfigureAwait(false);
            return throwOnError ? ir.ThrowOnError() : ir;
        }
    }
}