// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides <see cref="string"/> validation for an <b>e-mail</b>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    public class EmailRule<TEntity> : ValueRuleBase<TEntity, string?> where TEntity : class
    {
        private static readonly EmailAddressAttribute _emailChecker = new();

        /// <summary>
        /// Gets or sets the maximum length.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <inheritdoc/>
        public override Task ValidateAsync(PropertyContext<TEntity, string?> context, CancellationToken cancellation)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(context.Value))
                return Task.CompletedTask;

            if (!_emailChecker.IsValid(context.Value))
            { 
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.EmailFormat);
                return Task.CompletedTask;
            }

            if (MaxLength.HasValue && context.Value.Length > MaxLength.Value)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxLengthFormat, MaxLength);

            return Task.CompletedTask;
        }
    }
}