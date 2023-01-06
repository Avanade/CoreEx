// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation for a <see cref="IReferenceData.Code"/>; validates that the <see cref="IReferenceData.IsValid"/>.
    /// </summary>
    public class ReferenceDataCodeRule<TEntity, TRef> : ValueRuleBase<TEntity, string> where TEntity : class where TRef : IReferenceData?
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataCodeRule{TEntity, TRef}"/> class.
        /// </summary>
        public ReferenceDataCodeRule() => ValidateWhenDefault = false;

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken = default)
        {
            if (!ReferenceDataOrchestrator.Current.GetByTypeRequired<TRef>().TryGetByCode(context.Value!, out var rd) || !rd!.IsValid)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            return Task.CompletedTask;
        }
    }
}