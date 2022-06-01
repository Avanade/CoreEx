// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides a common value rule that can be used by other validators that share the same <see cref="Type"/> being <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <remarks>Note: the <see cref="PropertyRuleBase{TEntity, TProperty}.Name"/>, <see cref="PropertyRuleBase{TEntity, TProperty}.JsonName"/> and <see cref="PropertyRuleBase{TEntity, TProperty}.Text"/> initially default to <see cref="Validator.ValueNameDefault"/>.</remarks>
    public class CommonValidator<T> : PropertyRuleBase<ValidationValue<T>, T>, IValidatorEx<T>
    {
        private Func<PropertyContext<ValidationValue<T>, T>, CancellationToken, Task>? _additionalAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonValidator{T}"/>.
        /// </summary>
        public CommonValidator() : base(Validator.ValueNameDefault) { }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException"/>
        public override Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The ValidateAsync method is not supported for a CommonValueRule<T>.");

        /// <summary>
        /// Validates the value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name (defaults to <see cref="Validator.ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <param name="throwOnError">Indicates to throw a <see cref="ValidationException"/> where an error was found.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(T? value, string? name = null, LText? text = null, bool throwOnError = false, CancellationToken cancellationToken = default)
            => ValidateAsync(value, name, name, text, throwOnError, cancellationToken);

        /// <summary>
        /// Validates the value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name.</param>
        /// <param name="jsonName">The value JSON name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <param name="throwOnError">Indicates to throw a <see cref="ValidationException"/> where an error was found.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public async Task<ValueValidatorResult<ValidationValue<T>, T>> ValidateAsync(T? value, string? name, string? jsonName, LText? text = null, bool throwOnError = false, CancellationToken cancellationToken = default)
        {
            var vv = new ValidationValue<T>(null, value);
            var ctx = new PropertyContext<ValidationValue<T>, T>(new ValidationContext<ValidationValue<T>>(vv,
                new ValidationArgs()), value, name ?? Name, jsonName ?? JsonName, text ?? PropertyExpression.ConvertToSentenceCase(name) ?? Text);

            await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);
            var res = new ValueValidatorResult<ValidationValue<T>, T>(ctx);

            await OnValidateAsync(ctx, cancellationToken).ConfigureAwait(false);
            if (_additionalAsync != null)
                await _additionalAsync(ctx, cancellationToken).ConfigureAwait(false);

            if (throwOnError)
                res.ThrowOnError();

            return res;
        }

        /// <summary>
        /// Validates the value.
        /// </summary>
        /// <typeparam name="TEntity">The related entity <see cref="Type"/>.</typeparam>
        /// <param name="context">The related <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        internal async Task ValidateAsync<TEntity>(PropertyContext<TEntity, T> context, CancellationToken cancellationToken) where TEntity : class
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var vv = new ValidationValue<T>(context.Parent.Value, context.Value);
            var vc = new ValidationContext<ValidationValue<T>>(vv, new ValidationArgs
            {
                Config = context.Parent.Config,
                SelectedPropertyName = context.Name,
                FullyQualifiedEntityName = context.Parent.FullyQualifiedEntityName,
                FullyQualifiedJsonEntityName = context.Parent.FullyQualifiedJsonEntityName,
                UseJsonNames = context.UseJsonName
            });

            var ctx = new PropertyContext<ValidationValue<T>, T>(vc, context.Value, context.Name, context.JsonName, context.Text);
            await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);

            await OnValidateAsync(ctx, cancellationToken).ConfigureAwait(false);
            if (_additionalAsync != null)
                await _additionalAsync(ctx, cancellationToken).ConfigureAwait(false);

            context.HasError = ctx.HasError;
            context.Parent.MergeResult(ctx.Parent.Messages);
        }

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added by the inheriting classes.
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext{TEntity}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="Task"/>.</returns>
        protected virtual Task OnValidateAsync(PropertyContext<ValidationValue<T>, T> context, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added.
        /// </summary>
        /// <param name="additionalAsync">The asynchronous function to invoke.</param>
        /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
        public CommonValidator<T> Additional(Func<PropertyContext<ValidationValue<T>, T>, CancellationToken, Task> additionalAsync)
        {
            if (_additionalAsync != null)
                throw new InvalidOperationException("Additional can only be defined once for a Validator.");

            _additionalAsync = additionalAsync ?? throw new ArgumentNullException(nameof(additionalAsync));
            return this;
        }

        /// <inheritdoc/>
        async Task<ValidationContext<T>> IValidatorEx<T>.ValidateAsync(T? value, ValidationArgs? args, CancellationToken cancellationToken)
        {
            var context = new ValidationContext<T>(value, args ?? new ValidationArgs());
            var ir = await ValidateAsync(value, context.FullyQualifiedEntityName, context.FullyQualifiedEntityName, Text, cancellationToken: cancellationToken).ConfigureAwait(false);
            context.MergeResult(ir.Messages);
            return context;
        }
    }
}