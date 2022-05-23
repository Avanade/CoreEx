// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation where the rule predicate <b>must</b> return <c>true</c> to be considered valid.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class MustRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly Predicate<TEntity>? _predicate;
        private readonly Func<bool>? _must;
        private readonly Func<CancellationToken, Task<bool>>? _mustAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="MustRule{TEntity, TProperty}"/> class with a <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The must predicate.</param>
        public MustRule(Predicate<TEntity> predicate) => _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        /// <summary>
        /// Initializes a new instance of the <see cref="MustRule{TEntity, TProperty}"/> class with a <paramref name="must"/> function.
        /// </summary>
        /// <param name="must">The must function.</param>
        public MustRule(Func<bool> must) => _must = must ?? throw new ArgumentNullException(nameof(must));

        /// <summary>
        /// Initializes a new instance of the <see cref="MustRule{TEntity, TProperty}"/> class with a <paramref name="mustAsync"/> function.
        /// </summary>
        /// <param name="mustAsync">The must function.</param>
        public MustRule(Func<CancellationToken, Task<bool>> mustAsync) => _mustAsync = mustAsync ?? throw new ArgumentNullException(nameof(mustAsync));

        /// <inheritdoc/>
        public override async Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_predicate != null)
            {
                if (!_predicate.Invoke(context.Parent.Value!))
                    CreateErrorMessage(context);
            }
            else if (_must != null)
            {
                if (!_must.Invoke())
                    CreateErrorMessage(context);
            }
            else
            {
                if (!await _mustAsync!.Invoke(cancellationToken).ConfigureAwait(false))
                    CreateErrorMessage(context);
            }
        }

        /// <summary>
        /// Create the error message.
        /// </summary>
        private void CreateErrorMessage(PropertyContext<TEntity, TProperty> context) => context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MustFormat);
    }
}