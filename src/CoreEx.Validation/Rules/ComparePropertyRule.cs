﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Localization;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a comparision validation against another property within the same entity; also confirms other property has no errors prior to comparison.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TCompareProperty">The compare to property <see cref="Type"/>.</typeparam>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareToPropertyExpression">The <see cref="Expression"/> to reference the compare to entity property.</param>
    /// <param name="compareToText">The compare to text <see cref="LText"/> to be passed for the error message (default is to derive the text from the property itself).</param>
    public class ComparePropertyRule<TEntity, TProperty, TCompareProperty>(CompareOperator compareOperator, Expression<Func<TEntity, TCompareProperty>> compareToPropertyExpression, LText? compareToText = null) : CompareRuleBase<TEntity, TProperty>(compareOperator) where TEntity : class
    {
        private readonly PropertyExpression<TEntity, TCompareProperty> _compareTo = PropertyExpression.Create(compareToPropertyExpression);
        private readonly LText? _compareToText = compareToText;

        /// <inheritdoc/>
        protected override Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken = default)
        {
            // Do not validate where the compare to property has an error.
            if (context.Parent.HasError(_compareTo))
                return Task.CompletedTask;

            // Convert type and compare values.
            try
            {
                var compareToValue = (TProperty)(object)_compareTo.GetValue(context.Parent.Value)!;
                if (!Compare(context.Value!, compareToValue))
                    CreateErrorMessage(context, _compareToText ?? _compareTo.Text);

                return Task.CompletedTask;
            }
            catch (InvalidCastException icex)
            {
                throw new InvalidCastException($"Property '{_compareTo.Name}' and '{context.Name}' are incompatible: {icex.Message}", icex);
            }
        }
    }
}