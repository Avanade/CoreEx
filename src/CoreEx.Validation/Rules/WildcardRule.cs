// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Wildcards;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides <see cref="string"/> <see cref="Wildcard"/> validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    public class WildcardRule<TEntity> : ValueRuleBase<TEntity, string> where TEntity : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WildcardRule{TEntity}"/> class.
        /// </summary>
        public WildcardRule() => ValidateWhenDefault = false;

        /// <summary>
        /// Gets or sets the <see cref="Wildcard"/> configuration (uses <see cref="Wildcard.Default"/> where <c>null</c>).
        /// </summary>
        public Wildcard? Wildcard { get; set; }

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken = default)
        {
            var wildcard = Wildcard ?? Wildcard.Default ?? Wildcard.MultiBasic;
            if (wildcard != null && !wildcard.Validate(context.Value))
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.WildcardFormat);

            return Task.CompletedTask;
        }
    }
}