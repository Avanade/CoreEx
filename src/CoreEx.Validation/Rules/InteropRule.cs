// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides interop validation to a base <see cref="IValidator"/> (intended for non-<c>CoreEx.Validation</c>).
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
    public class InteropRule<TEntity, TProperty, TValidator> : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : class? where TValidator : IValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityRule{TEntity, TProperty, TValidator}"/> class.
        /// </summary>
        /// <param name="validatorFunc">The function to return the <see cref="IValidator"/>.</param>
        public InteropRule(Func<TValidator> validatorFunc)
        {
            ValidatorFunc = validatorFunc.ThrowIfNull(nameof(validatorFunc));
            if (validatorFunc is IValidatorEx)
                throw new ArgumentException($"{ValidatorFunc.GetType().Name} implements {typeof(IValidatorEx).Name} and as such must use {typeof(EntityRule<,,>).Name}.", nameof(validatorFunc));
        }

        /// <summary>
        /// Gets the <see cref="IValidatorEx"/> function.
        /// </summary>
        public Func<TValidator> ValidatorFunc { get; private set; }

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

            var v = ValidatorFunc() ?? throw new InvalidOperationException($"The {nameof(ValidatorFunc)} must return a non-null value.");
            if (v is IValidatorEx vex) // Use the "better" validator to enable.
            {
                // Create the context args.
                var args = context.CreateValidationArgs();

                // Validate and merge.
                context.MergeResult(await vex.ValidateAsync(context.Value, args, cancellationToken).ConfigureAwait(false));
                return;
            }

            // Validate and merge using basic "interop".
            var ir = await v.ValidateAsync(context.Value, cancellationToken).ConfigureAwait(false);

            if (ir.HasErrors)
                context.HasError = true;

            context.Parent.MergeResult(ir.Messages);
        }
    }
}