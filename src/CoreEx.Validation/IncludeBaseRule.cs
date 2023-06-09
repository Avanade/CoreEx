// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents a rule that enables a base validator to be included.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TInclude">The entity base <see cref="Type"/>.</typeparam>
    public class IncludeBaseRule<TEntity, TInclude> : ValidatorBase<TEntity>, IPropertyRule<TEntity> where TEntity : class where TInclude : class
    {
        private readonly IValidatorEx<TInclude> _include;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeBaseRule{TEntity, TInclude}"/> class.
        /// </summary>
        /// <param name="include">The base <see cref="IValidatorEx{TInclude}"/>.</param>
        internal IncludeBaseRule(IValidatorEx<TInclude> include) => _include = include ?? throw new ArgumentNullException(nameof(include));

        /// <inheritdoc/>
        public async Task ValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Value is not TInclude val)
                throw new InvalidOperationException($"Type {typeof(TEntity).Name} must inherit from {typeof(TInclude).Name}.");

            var ctx = new ValidationContext<TInclude>(val, new ValidationArgs
            {
                Config = context.Config,
                SelectedPropertyName = context.SelectedPropertyName,
                ShallowValidation = context.ShallowValidation,
                FullyQualifiedEntityName = context.FullyQualifiedEntityName,
                UseJsonNames = context.UsedJsonNames
            });

            if (_include is ValidatorBase<TInclude> vb) // Victoria Bitter, for a hard-earned thirst: https://www.youtube.com/watch?v=WA1h9h7-_Z4
            {
                foreach (var r in vb.Rules)
                {
                    await r.ValidateAsync(ctx, cancellationToken).ConfigureAwait(false);
                    if (ctx.FailureResult is not null)
                        break;
                }
            }

            context.MergeResult(ctx);
        }
    }
}