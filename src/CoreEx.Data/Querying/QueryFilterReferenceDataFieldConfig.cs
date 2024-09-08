// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the <see cref="QueryFilterParser"/> <see cref="IReferenceData"/> field configuration.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <remarks>This will automatically set the <see cref="QueryFilterFieldConfigBase.SupportedKinds"/> to be <see cref="QueryFilterTokenKind.EqualityOperator"/> only.</remarks>
    public class QueryFilterReferenceDataFieldConfig<TRef> : QueryFilterFieldConfigBase<QueryFilterFieldConfig<TRef>> where TRef : IReferenceData, new()
    {
        private bool _useIdentifier;
        private bool _mustBeValid = true;
        private Func<TRef, TRef>? _valueFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterReferenceDataFieldConfig{TRef}"/> class.
        /// </summary>
        /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
        /// <param name="field">The field name.</param>
        /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
        public QueryFilterReferenceDataFieldConfig(QueryFilterParser parser, string field, string? model) : base(parser, typeof(TRef), field, model)
        {
            SupportedKinds = QueryFilterTokenKind.EqualityOperator;
            IsTypeString = true;
        }

        /// <summary>
        /// Indicates that the <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> is to be used as the value for the query (versus the originating filter value being the <see cref="IReferenceData.Code"/>).
        /// </summary>
        /// <returns>The <see cref="QueryFilterFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This will automatically set the <see cref="QueryFilterFieldConfigBase.SupportedKinds"/> to be <see cref="QueryFilterTokenKind.EqualityOperator"/> only as other operators are nonsensical in this context.</remarks>
        public QueryFilterReferenceDataFieldConfig<TRef> UseIdentifier()
        {
            _useIdentifier = true;
            return this;
        }

        /// <summary>
        /// Indicates that the resulting converted value must be <see cref="IReferenceData.IsValid"/>.
        /// </summary>
        /// <param name="mustBeValid"><see langword="true"/> indicates that an error will occur where not valid; otherwise, <see langword="false"/>.</param>
        /// <returns>The <see cref="QueryFilterFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Defaults to <see langword="true"/>.</remarks>
        public QueryFilterReferenceDataFieldConfig<TRef> MustBeValid(bool mustBeValid = true)
        {
            _mustBeValid = mustBeValid;
            return this;
        }

        /// <summary>
        /// Sets (overrides) the <paramref name="value"/> function to, a) further convert the field <typeparamref name="TRef"/> value; and/or, b) to provide additional validation.
        /// </summary>
        /// <param name="value">The value function.</param>
        /// <returns>The final value that will be used in the LINQ query.</returns>
        /// <remarks>This is an opportunity to further validate the query as needed. Throw a <see cref="FormatException"/> to have the validation message formatted correctly and consistently.</remarks>
        public QueryFilterReferenceDataFieldConfig<TRef> WithValue(Func<TRef, TRef>? value)
        {
            _valueFunc = value;
            return this;
        }

        /// <inheritdoc/>
        protected override object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter)
        {
            var text = field.GetValueToken(filter);
            TRef value = ReferenceDataOrchestrator.ConvertFromCode<TRef>(text);

            if (_mustBeValid && !value.IsValid)
                throw new FormatException("Reference data code is invalid.");

            if (_valueFunc is not null)
                value = _valueFunc.Invoke(value) ?? throw new FormatException("Reference data code is invalid.");

            return _useIdentifier
                ? (value.Id is null ? string.Empty : value.Id)
                : value.Code!;
        }
    }
}