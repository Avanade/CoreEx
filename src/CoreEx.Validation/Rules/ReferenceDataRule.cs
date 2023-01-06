// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation for a <see cref="IReferenceData"/>; validates that the <see cref="IReferenceData.IsValid"/>.
    /// </summary>
    public class ReferenceDataRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IReferenceData?
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataRule{TEntity, TProperty}"/> class.
        /// </summary>
        public ReferenceDataRule() => ValidateWhenDefault = false;

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (!context.Value!.IsValid)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            return Task.CompletedTask;
        }
    }
}