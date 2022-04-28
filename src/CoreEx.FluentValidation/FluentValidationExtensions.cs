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
        /// Defines an <see cref="IReferenceData"/> validator whereby the <see cref="ReferenceDataTypeOf{T}.TypeOf{TRef}"/> is required to specify corresponding <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The owning object <see cref="Type"/>.</typeparam>
        /// <param name="ruleBuilder">The <paramref name="ruleBuilder"/> to enable the extension method.</param>
        /// <returns></returns>
        public static ReferenceDataTypeOf<T> ReferenceData<T>(this IRuleBuilder<T, string?> ruleBuilder) => new(ruleBuilder);

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
            public IRuleBuilderOptions<T, string?> TypeOf<TRef>() where TRef : IReferenceData => _ruleBuilder.SetValidator(new ReferenceDataValidator<T, TRef>()); 
        }
    }
}