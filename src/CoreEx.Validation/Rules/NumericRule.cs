// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Represents a numeric rule to validate whether negatives are allowed.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class NumericRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        /// <summary>
        /// Indicates whether to allow negative values.
        /// </summary>
        public bool AllowNegatives { get; set; }

        /// <inheritdoc/>
        public override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Where allowing negatives or the value is null, do nothing; i.e. Nullable<Type>.
            if (AllowNegatives || Comparer<object>.Default.Compare(context.Value!, null!) == 0)
                return Task.CompletedTask;

            // Convert numeric to a double value.
            double value = Convert.ToDouble(context.Value, System.Globalization.CultureInfo.InvariantCulture);

            // Determine if the value is negative and is/isn't allowed.
            if (!AllowNegatives && value < 0)
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.AllowNegativesFormat);

            return Task.CompletedTask;
        }
    }
}