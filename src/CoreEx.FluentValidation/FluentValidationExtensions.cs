// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.FluentValidation;
using CoreEx.RefData;
using System;

namespace FluentValidation
{
    /// <summary>
    /// <c>FluentValidation</c> extension methods.
    /// </summary>
    public static class FluentValidationExtensions
    {
        /// <summary>
        /// Defines an <see cref="IReferenceData"/> validator whereby the <see cref="ReferenceDataTypeOf{T}.As{TRef}"/> is required to specify corresponding <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The owning object <see cref="Type"/>.</typeparam>
        /// <param name="ruleBuilder">The <paramref name="ruleBuilder"/> to enable the extension method.</param>
        /// <returns></returns>
        public static ReferenceDataTypeOf<T> RefDataCode<T>(this IRuleBuilder<T, string?> ruleBuilder) => new(ruleBuilder);

        /// <summary>
        /// Provides for the specification of the corresponding <see cref="ReferenceDataTypeOf{T}"/> <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The owning object <see cref="Type"/>.</typeparam>
        public class ReferenceDataTypeOf<T>
        {
            private readonly IRuleBuilder<T, string?> _ruleBuilder;

            /// <summary>
            /// Initializes a new instance of the <see cref="ReferenceDataTypeOf{T}"/> class.
            /// </summary>
            /// <param name="ruleBuilder">The <see cref="IRuleBuilder{T, TProperty}"/>.</param>
            internal ReferenceDataTypeOf(IRuleBuilder<T, string?> ruleBuilder) => _ruleBuilder = ruleBuilder;

            /// <summary>
            /// Sets the <see cref="ReferenceDataValidator{T, TRef}"/> for the specified <typeparamref name="TRef"/> <see cref="Type"/>.
            /// </summary>
            /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
            /// <returns>The <see cref="IRuleBuilderOptions{T, TProperty}"/> to support fluent-style method-chaining.</returns>
            public IRuleBuilderOptions<T, string?> As<TRef>() where TRef : IReferenceData => _ruleBuilder.SetValidator(new ReferenceDataValidator<T, TRef>()); 
        }

        /// <summary>
        /// Associates a validator provider with the current property rule where the property is nullable (and therefore optional).
        /// </summary>
        /// <typeparam name="T">The owning object <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">the property <see cref="Type"/>.</typeparam>
        /// <param name="ruleBuilder">The <paramref name="ruleBuilder"/> to enable the extension method.</param>
        /// <param name="validator">The property <see cref="IValidator{T}"/>.</param>
        /// <param name="ruleSets">The list of rule sets.</param>
        /// <returns>The <see cref="IRuleBuilderOptions{T, TProperty}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Added as advised: <see href="https://github.com/FluentValidation/FluentValidation/issues/1320"/>.</remarks>
        public static IRuleBuilderOptions<T, TProperty> SetOptionalValidator<T, TProperty>(this IRuleBuilder<T, TProperty?> ruleBuilder, IValidator<TProperty> validator, params string[] ruleSets) where TProperty : class
            => ruleBuilder.SetValidator(validator!, ruleSets)!;

        /// <summary>
        /// Wraps the <i>FluentValidation</i> <see cref="IValidator{T}"/> to a <see cref="CoreEx.Validation.IValidator{T}"/> using a <see cref="ValidatorWrapper{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="validator">The <i>FluentValidation</i> <see cref="IValidator{T}"/>.</param>
        /// <returns>The <see cref="CoreEx.Validation.IValidator{T}"/>.</returns>
        public static CoreEx.Validation.IValidator<T> Wrap<T>(this IValidator<T> validator) => new ValidatorWrapper<T>(validator);
    }
}