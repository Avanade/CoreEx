// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides validation for a <see cref="IReferenceData"/>; validates that the <see cref="IReferenceData.IsValid"/>.
    /// </summary>
    public class ReferenceDataRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IReferenceData?
    {
        /// <inheritdoc/>
        public override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Value == null)
                return Task.CompletedTask;

            if (!context.Value.IsValid)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            return Task.CompletedTask;
        }
    }
}