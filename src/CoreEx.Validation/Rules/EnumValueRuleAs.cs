// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a means to add an <see cref="EnumValueRule{TEntity, TEnum}"/> <see cref="As"/> a specified <see cref="Enum"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public class EnumValueRuleAs<TEntity> where TEntity : class
    {
        private readonly IPropertyRule<TEntity, string> _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumValueRuleAs{TEntity}"/> class.
        /// </summary>
        /// <param name="parent">The parent <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
        public EnumValueRuleAs(IPropertyRule<TEntity, string> parent) => _parent = parent ?? throw new ArgumentNullException(nameof(parent));

        /// <summary>
        /// Adds an <see cref="EntityRule{TEntity, TProperty, TValidator}"/> using a validator <see cref="As"/> a specified <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The property <see cref="Enum"/> <see cref="Type"/>.</typeparam>
        /// <param name="ignoreCase">Indicates whether to ignore the casing of the value when parsing the <typeparamref name="TEnum"/>.</param>
        /// <param name="overrideValue">Indicates whether to override the underlying property value with the corresponding <see cref="Enum"/> name.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public IPropertyRule<TEntity, string> As<TEnum>(bool ignoreCase = false, bool overrideValue = false, LText? errorText = null) where TEnum : struct, Enum
        {
            _parent.AddRule(new EnumValueRule<TEntity, TEnum> { IgnoreCase = ignoreCase, OverrideValue = overrideValue, ErrorText = errorText });
            return _parent;
        }
    }
}