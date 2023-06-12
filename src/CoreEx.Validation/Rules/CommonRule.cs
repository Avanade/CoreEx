// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides for integrating a common validation against a specified property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
    internal class CommonRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly CommonValidator<TProperty> _commonValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonRule{TEntity, TProperty}"/> class specifying the corresponding <paramref name="commonValidator"/>.
        /// </summary>
        /// <param name="commonValidator">The <see cref="CommonValidator{T}"/>.</param>
        public CommonRule(CommonValidator<TProperty> commonValidator) => _commonValidator = commonValidator ?? throw new ArgumentNullException(nameof(commonValidator));

        /// <inheritdoc/>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
            => await _commonValidator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
    }
}