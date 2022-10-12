// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using CoreEx.RefData;
using System;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a means to add an <see cref="ReferenceDataCodeRule{TEntity, TRefData}"/> using a validator <see cref="As"/> a specified <see cref="IReferenceData"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public class ReferenceDataCodeRuleAs<TEntity> where TEntity : class
    {
        private readonly IPropertyRule<TEntity, string> _parent;
        private readonly LText? _errorText;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityRuleWith{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="parent">The parent <see cref="PropertyRuleBase{TEntity, TProperty}"/>.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        public ReferenceDataCodeRuleAs(IPropertyRule<TEntity, string> parent, LText? errorText = null)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _errorText = errorText;
        }

        /// <summary>
        /// Adds an <see cref="ReferenceDataCodeRule{TEntity, TRefData}"/> using a validator <see cref="As"/> a specified <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <returns>A <see cref="ReferenceDataCodeRule{TEntity, TRefData}"/>.</returns>
        public IPropertyRule<TEntity, string> As<TRef>() where TRef : IReferenceData
        {
            _parent.AddRule(new ReferenceDataCodeRule<TEntity, TRef>() { ErrorText = _errorText });
            return _parent;
        }
    }
}