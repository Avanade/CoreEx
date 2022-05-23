// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation for a <see cref="IReferenceData.Code"/>; validates that the <see cref="IReferenceData.IsValid"/>.
    /// </summary>
    public class ReferenceDataCodeRule<TEntity, TRef> : ValueRuleBase<TEntity, string?> where TEntity : class where TRef : IReferenceData?
    {
        /// <inheritdoc/>
        public override Task ValidateAsync(PropertyContext<TEntity, string?> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Value == null)
                return Task.CompletedTask;

            if (!ReferenceDataOrchestrator.Current.GetByTypeRequired<TRef>().TryGetByCode(context.Value, out var rd) && rd!.IsValid)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            return Task.CompletedTask;
        }
    }
}