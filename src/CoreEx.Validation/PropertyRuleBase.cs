// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Localization;
using CoreEx.Validation.Clauses;
using CoreEx.Validation.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents a base validation rule for an entity property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public abstract class PropertyRuleBase<TEntity, TProperty> where TEntity : class
    {
        private readonly List<IValueRule<TEntity, TProperty>> _rules = new();
        private readonly List<IPropertyRuleClause<TEntity>> _clauses = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRuleBase{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as <see cref="Abstractions.Reflection.PropertyExpression.ToSentenceCase(string)"/>).</param>
        /// <param name="jsonName">The JSON property name (defaults to <paramref name="name"/>).</param>
        protected PropertyRuleBase(string name, LText? text = null, string? jsonName = null)
        {
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentNullException(nameof(name)) : name;
            Text = text ?? Name.ToSentenceCase();
            JsonName = string.IsNullOrEmpty(jsonName) ? Name : jsonName;
        }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        public string JsonName { get; internal set; }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        public LText Text { get; set; }

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
            if (context == null)
                throw new ArgumentNullException(nameof(context));

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
                if (context.HasError)
                    break;
            }
        }

        /// <summary>
        /// Executes the validation for the property value.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public abstract Task<ValueValidatorResult<TEntity, TProperty>> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> where the <typeparamref name="TEntity"/> <paramref name="predicate"/> must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="predicate">A function to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> When(Predicate<TEntity> predicate)
        {
            if (predicate == null)
                return this;

            AddClause(new WhenClause<TEntity, TProperty>(predicate));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> where the <typeparamref name="TProperty"/> <paramref name="predicate"/> must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="predicate">A function to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> WhenValue(Predicate<TProperty> predicate)
        {
            if (predicate == null)
                return this;

            AddClause(new WhenClause<TEntity, TProperty>(predicate));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> where the <typeparamref name="TProperty"/> must have a value (i.e. not the default value for the Type) for the rule to be validated.
        /// </summary>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> WhenHasValue() => WhenValue((TProperty pv) => Comparer<TProperty>.Default.Compare(pv, default!) != 0);

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> which must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="when">A function to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> When(Func<bool> when)
        {
            if (when == null)
                return this;

            AddClause(new WhenClause<TEntity, TProperty>(when));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> which must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="when">A <see cref="Boolean"/> to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> When(bool when)
        {
            AddClause(new WhenClause<TEntity, TProperty>(() => when));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> that states that the
        /// <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext"/> <see cref="ExecutionContext.OperationType"/> is equal to the specified
        /// (<paramref name="operationType"/>).
        /// </summary>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> WhenOperation(OperationType operationType) => When(x => ExecutionContext.Current.OperationType == operationType);

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> that states that the
        /// <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext"/> <see cref="ExecutionContext.OperationType"/> is not equal to the specified
        /// (<paramref name="operationType"/>).
        /// </summary>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> WhenNotOperation(OperationType operationType) => When(x => ExecutionContext.Current.OperationType != operationType);

        /// <summary>
        /// Adds a <see cref="DependsOnClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> which must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="expression">A depends on expression.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> DependsOn<TDependsProperty>(Expression<Func<TEntity, TDependsProperty>> expression)
        {
            if (expression == null)
                return this;

            AddClause(new DependsOnClause<TEntity, TDependsProperty>(expression));
            return this;
        }
    }
}