// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation where the immutable rule predicate <b>must</b> return <c>true</c> to be considered valid.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class ImmutableRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly Predicate<TEntity>? _predicate;
        private readonly Func<bool>? _immutable;
        private readonly Func<CancellationToken, Task<bool>>? _immutableAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableRule{TEntity, TProperty}"/> class with a <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The must predicate.</param>
        public ImmutableRule(Predicate<TEntity> predicate) => _predicate = predicate.ThrowIfNull(nameof(predicate));

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableRule{TEntity, TProperty}"/> class with an <paramref name="immutable"/> function.
        /// </summary>
        /// <param name="immutable">The immutable function.</param>
        public ImmutableRule(Func<bool> immutable) => _immutable = immutable.ThrowIfNull(nameof(immutable));

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableRule{TEntity, TProperty}"/> class with an <paramref name="immutableAsync"/> function.
        /// </summary>
        /// <param name="immutableAsync">The immutable function.</param>
        public ImmutableRule(Func<CancellationToken, Task<bool>> immutableAsync) => _immutableAsync = immutableAsync.ThrowIfNull(nameof(immutableAsync));

        /// <inheritdoc/>
        protected override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (_predicate != null)
            {
                if (!_predicate.Invoke(context.Parent.Value!))
                    CreateErrorMessage(context);
            }
            else if (_immutable != null)
            {
                if (!_immutable.Invoke())
                    CreateErrorMessage(context);
            }
            else 
            {
                if (!await _immutableAsync!.Invoke(cancellationToken).ConfigureAwait(false))
                    CreateErrorMessage(context);
            }
        }

        /// <summary>
        /// Create the error message.
        /// </summary>
        private void CreateErrorMessage(PropertyContext<TEntity, TProperty> context) => context.CreateErrorMessage(ErrorText ?? ValidatorStrings.ImmutableFormat);
    }
}