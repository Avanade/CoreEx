// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides <see cref="string"/> validation including <see cref="MinLength"/>, <see cref="MaxLength"/>, and <see cref="Regex"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    public class StringRule<TEntity> : ValueRuleBase<TEntity, string> where TEntity : class
    {
        /// <summary>
        /// Gets or sets the minimum length;
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the regex.
        /// </summary>
        public Regex? Regex { get; set; }

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(context.Value))
                return Task.CompletedTask;

            if (context.Value.Length < MinLength)
            {
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MinLengthFormat, MinLength);
                return Task.CompletedTask;
            }

            if (MaxLength.HasValue && context.Value.Length > MaxLength.Value)
            {
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MaxLengthFormat, MaxLength);
                return Task.CompletedTask;
            }

            if (Regex != null && !Regex.IsMatch(context.Value))
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.RegexFormat);

            return Task.CompletedTask;
        }
    }
}