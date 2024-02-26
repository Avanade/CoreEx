// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides entity validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
    /// <param name="validator">The <see cref="IValidatorEx"/>.</param>
    public class EntityRule<TEntity, TProperty, TValidator>(TValidator validator) : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : class? where TValidator : IValidatorEx
    {
        /// <summary>
        /// Gets the <see cref="IValidatorEx"/>.
        /// </summary>
        public TValidator Validator { get; private set; } = validator.ThrowIfNull(nameof(validator));

        /// <summary>
        /// Overrides the <b>Check</b> method and will not validate where performing a shallow validation.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <returns><c>true</c> where validation is to continue; otherwise, <c>false</c> to stop.</returns>
        protected override bool Check(PropertyContext<TEntity, TProperty> context) => !context.ThrowIfNull(nameof(context)).Parent.ShallowValidation && base.Check(context);

        /// <inheritdoc/>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            // Exit where nothing to validate.
            if (context.Value == null)
                return;

            // Create the context args.
            var args = context.CreateValidationArgs();

            // Validate and merge.
            context.MergeResult(await Validator.ValidateAsync(context.Value, args, cancellationToken).ConfigureAwait(false));
        }
    }
}