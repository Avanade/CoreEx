﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System;
using System.Collections.Generic;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides base comparision validation capability.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public abstract class CompareRuleBase<TEntity, TProperty> : ValueRuleBase<TEntity, TProperty> where TEntity : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompareRuleBase{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
        protected CompareRuleBase(CompareOperator compareOperator) => Operator = compareOperator;

        /// <summary>
        /// Gets the <see cref="CompareOperator"/>.
        /// </summary>
        public CompareOperator Operator { get; private set; }

        /// <summary>
        /// Gets or sets the comparer.
        /// </summary>
        public Comparer<TProperty?> Comparer { get; set; } = Comparer<TProperty?>.Default;

        /// <summary>
        /// Compare two values using the default comparer for the type.
        /// </summary>
        /// <param name="lValue">The left value.</param>
        /// <param name="rValue">The right value.</param>
        /// <returns><c>true</c> where valid; otherwise, <c>false</c>.</returns>
        protected bool Compare(TProperty? lValue, TProperty? rValue) => Operator switch
        {
            CompareOperator.Equal => Comparer.Compare(lValue, rValue) == 0,
            CompareOperator.NotEqual => Comparer.Compare(lValue, rValue) != 0,
            CompareOperator.LessThan => Comparer.Compare(lValue, rValue) < 0,
            CompareOperator.LessThanEqual => Comparer.Compare(lValue, rValue) <= 0,
            CompareOperator.GreaterThan => Comparer.Compare(lValue, rValue) > 0,
            CompareOperator.GreaterThanEqual => Comparer.Compare(lValue, rValue) >= 0,
            _ => throw new InvalidOperationException("An invalid Operator value was encountered.")
        };

        /// <summary>
        /// Creates the error message passing the <paramref name="compareToText"/> text as the third format parameter (i.e. String.Format("{2}")).
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="compareToText">The compare text <see cref="LText"/> to be passed for the error message.</param>
        protected void CreateErrorMessage(PropertyContext<TEntity, TProperty> context, LText compareToText)
        {
            context.ThrowIfNull(nameof(context));

            switch (Operator)
            {
                case CompareOperator.Equal: context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CompareEqualFormat, (string)compareToText); break;
                case CompareOperator.NotEqual: context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CompareNotEqualFormat, (string)compareToText); break;
                case CompareOperator.LessThan: context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CompareLessThanFormat, (string)compareToText); break;
                case CompareOperator.LessThanEqual: context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CompareLessThanEqualFormat, (string)compareToText); break;
                case CompareOperator.GreaterThan: context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CompareGreaterThanFormat, (string)compareToText); break;
                case CompareOperator.GreaterThanEqual: context.CreateErrorMessage(ErrorText ?? ValidatorStrings.CompareGreaterThanEqualFormat, (string)compareToText); break;
            }
        }
    }
}