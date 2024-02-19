// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using CoreEx.Validation.Clauses;
using CoreEx.Validation.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents a base validation rule for an entity property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public abstract class PropertyRuleBase<TEntity, TProperty> : IPropertyRule<TEntity, TProperty> where TEntity : class
    {
        private readonly List<IValueRule<TEntity, TProperty>> _rules = [];
        private readonly List<IPropertyRuleClause<TEntity>> _clauses = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRuleBase{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as <see cref="Abstractions.Reflection.PropertyExpression.ToSentenceCase(string)"/>).</param>
        /// <param name="jsonName">The JSON property name (defaults to <paramref name="name"/>).</param>
        protected PropertyRuleBase(string name, LText? text = null, string? jsonName = null)
        {
            Name = name.ThrowIfNullOrEmpty(nameof(name));
            Text = text ?? Name.ToSentenceCase()!;
            JsonName = string.IsNullOrEmpty(jsonName) ? Name : jsonName;
        }

        /// <inheritdoc/>
        public string Name { get; internal set; }

        /// <inheritdoc/>
        public string JsonName { get; internal set; }

        /// <inheritdoc/>
        public LText Text { get; set; }

        /// <inheritdoc/>
        IPropertyRule<TEntity, TProperty> IPropertyRule<TEntity, TProperty>.AddRule(IValueRule<TEntity, TProperty> rule) => AddRule(rule);

        /// <summary>
        /// Adds a rule (<see cref="IValueRule{TEntity, TProperty}"/>) to the property.
        /// </summary>
        /// <param name="rule">The <see cref="IValueRule{TEntity, TProperty}"/>.</param>
        /// <returns>The <see cref="PropertyRuleBase{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> AddRule(IValueRule<TEntity, TProperty> rule)
        {
            _rules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds a clause (<see cref="IPropertyRuleClause{TEntity, TProperty}"/>) to the rule.
        /// </summary>
        /// <param name="clause">The <see cref="IPropertyRuleClause{TEntity, TProperty}"/>.</param>
        public void AddClause(IPropertyRuleClause<TEntity> clause)
        {
            if (clause == null)
                return;

            if (_rules.Count == 0)
                _clauses.Add(clause);
            else
                _rules.Last().AddClause(clause);
        }

        /// <summary>
        /// Runs the configured clauses and rules.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected async Task InvokeAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
        {
            context.ThrowIfNull(nameof(context));

            // Check all "this" clauses.
            foreach (var clause in _clauses)
            {
                if (!clause.Check(context))
                    return;
            }

            // Check and execute all rules/clauses within the rules stack.
            foreach (var rule in _rules)
            {
                if (rule.Check(context))
                    await rule.ValidateAsync(context, cancellationToken).ConfigureAwait(false);

                // Stop validating after an error.
                if (context.HasError || context.Parent.FailureResult.HasValue)
                    break;
            }
        }

        /// <summary>
        /// Executes the validation for the property value.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public abstract Task<ValueValidatorResult<TEntity, TProperty>> ValidateAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        async Task<IValidationResult> IPropertyRule<TEntity, TProperty>.ValidateAsync(CancellationToken cancellationToken) => await ValidateAsync(cancellationToken).ConfigureAwait(false);
    }
}