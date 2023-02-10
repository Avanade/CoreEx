// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides <see cref="string"/> validation against an <see cref="Enum"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TEnum">The corresponding <see cref="Enum"/> <see cref="Type"/>.</typeparam>
    public class EnumValueRule<TEntity, TEnum> : ValueRuleBase<TEntity, string> where TEntity : class where TEnum : struct, Enum
    {
        /// <summary>
        /// Initializes a new <see cref="EnumValueRule{TEntity, TEnum}"/> class.
        /// </summary>
        public EnumValueRule() => ValidateWhenDefault = false;

        /// <summary>
        /// Indicates whether to ignore the casing of the value when parsing the <typeparamref name="TEnum"/>.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Indicates whether to override the underlying property value with the corresponding <see cref="Enum"/> name.
        /// </summary>
        /// <remarks>This is only applicable where <see cref="IgnoreCase"/> is true.</remarks>
        public bool OverrideValue { get; set; }

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellation)
        {
            if (!Enum.TryParse<TEnum>(context.Value!, IgnoreCase, out var val))
                context.CreateErrorMessage(ErrorText ?? ValidatorStrings.InvalidFormat);

            if (IgnoreCase && OverrideValue)
                context.OverrideValue(val.ToString());

            return Task.CompletedTask;
        }
    }
}