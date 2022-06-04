// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Wildcards;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides <see cref="string"/> <see cref="Wildcard"/> validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    public class WildcardRule<TEntity> : ValueRuleBase<TEntity, string?> where TEntity : class
    {
        /// <summary>
        /// Gets or sets the <see cref="Wildcard"/> configuration (uses <see cref="Wildcard.Default"/> where <c>null</c>).
        /// </summary>
        public Wildcard? Wildcard { get; set; }

        /// <inheritdoc/>
        public override Task ValidateAsync(PropertyContext<TEntity, string?> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(context.Value))
                return Task.CompletedTask;

            var wildcard = Wildcard ?? Wildcard.Default ?? Wildcard.MultiAll;
            if (wildcard != null && !wildcard.Validate(context.Value))
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.WildcardFormat);

            return Task.CompletedTask;
        }
    }
}