// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides mandatory validation; determined as mandatory when it equals its default value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <remarks>A value will be determined as mandatory when it equals its default value. For example an <see cref="int"/> will trigger when the value is zero; however, a
    /// <see cref="Nullable{Int32}"/> will trigger when null only (a zero is considered a value in this instance).
    /// <para>For a <see cref="string"/> mandatory is determined by <see cref="string.Length"/> being zero or <see cref="string.IsNullOrWhiteSpace(string?)"/> resulting in <c>true</c>.</para></remarks>
    public class MandatoryRule<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            // Compare the value against its default.
            if (Comparer<TProperty?>.Default.Compare(context.Value, default!) == 0)
                return CreateErrorMessageAsync(context);

            // Also check for empty strings.
            if (context.Value is string val && (val.Length == 0 || string.IsNullOrWhiteSpace(val)))
                return CreateErrorMessageAsync(context);

            // Also check for empty collections.
            if (context.Value is ICollection coll && coll.Count == 0)
                return CreateErrorMessageAsync(context);

            // Also check for empty enumerables.
            if (context.Value is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                if (!enumerator.MoveNext())
                    return CreateErrorMessageAsync(context);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Create the error message.
        /// </summary>
        private Task CreateErrorMessageAsync(PropertyContext<TEntity, TProperty> context)
        {
            context.CreateErrorMessage(ErrorText ?? ValidatorStrings.MandatoryFormat);
            return Task.CompletedTask;
        }
    }
}