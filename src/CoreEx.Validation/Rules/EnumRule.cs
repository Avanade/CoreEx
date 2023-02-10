// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides <see cref="Enum"/> validation to ensure that the value has been defined.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class EnumRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class where TProperty : struct, Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumRule{TEntity, TProperty}"/> class.
        /// </summary>
        public EnumRule() => ValidateWhenDefault = false;

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            // Make sure the enum is defined.
            if (!Enum.IsDefined(typeof(TProperty), context.Value))
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            return Task.CompletedTask;
        }
    }
}