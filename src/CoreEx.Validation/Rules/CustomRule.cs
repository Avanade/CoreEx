// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a custom validation against a specified property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
    public class CustomRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly Func<PropertyContext<TEntity, TProperty>, Result>? _custom;
        private readonly Func<PropertyContext<TEntity, TProperty>, CancellationToken, Task<Result>>? _customAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomRule{TEntity, TProperty}"/> class specifying the corresponding <paramref name="custom"/>.
        /// </summary>
        /// <param name="custom">The function to invoke to perform the custom validation.</param>
        public CustomRule(Func<PropertyContext<TEntity, TProperty>, Result> custom) => _custom = custom.ThrowIfNull(nameof(custom));

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomRule{TEntity, TProperty}"/> class specifying the corresponding <paramref name="customAsync"/>.
        /// </summary>
        /// <param name="customAsync">The function to invoke to perform the custom validation.</param>
        public CustomRule(Func<PropertyContext<TEntity, TProperty>, CancellationToken, Task<Result>> customAsync) => _customAsync = customAsync.ThrowIfNull(nameof(customAsync));

        /// <summary>
        /// Validate the property value.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            Result result;
            if (_customAsync == null)
                result = _custom!(context);
            else
                result = await _customAsync(context, cancellationToken).ConfigureAwait(false);

            context.Parent.SetFailureResult(result);
        }
    }
}