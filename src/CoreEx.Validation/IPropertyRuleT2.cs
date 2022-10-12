// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using CoreEx.Validation.Clauses;
using CoreEx.Validation.Rules;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables a validation rule for an entity property. 
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public interface IPropertyRule<TEntity, in TProperty> where TEntity : class
    {
        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the JSON property name.
        /// </summary>
        public string JsonName { get; }

        /// <summary>
        /// Gets or sets the friendly text name used in validation messages.
        /// </summary>
        public LText Text { get; set; }

        /// <summary>
        /// Adds a clause (<see cref="IPropertyRuleClause{TEntity, TProperty}"/>) to the rule.
        /// </summary>
        /// <param name="clause">The <see cref="IPropertyRuleClause{TEntity, TProperty}"/>.</param>
        void AddClause(IPropertyRuleClause<TEntity> clause);

        /// <summary>
        /// Adds a rule (<see cref="IValueRule{TEntity, TProperty}"/>) to the property.
        /// </summary>
        /// <param name="rule">The <see cref="IValueRule{TEntity, TProperty}"/>.</param>
        /// <returns>The <see cref="PropertyRuleBase{TEntity, TProperty}"/>.</returns>
        IPropertyRule<TEntity, TProperty> AddRule(IValueRule<TEntity, TProperty> rule);

        /// <summary>
        /// Executes the validation for the property value.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        Task<IValidationResult> ValidateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the validation for the property value.
        /// </summary>
        /// <param name="throwOnError">Indicates whether to automatically throw a <see cref="ValidationException"/> where <see cref="IValidationResult.HasErrors"/>.</param>
        /// <param name="cancellationToken">>The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="ValueValidatorResult{TEntity, TProperty}"/>.</returns>
        public async Task<IValidationResult> ValidateAsync(bool throwOnError, CancellationToken cancellationToken = default)
        {
            var ir = await ValidateAsync(cancellationToken).ConfigureAwait(false);
            return throwOnError ? ir.ThrowOnError() : ir;
        }

        /// <summary>
        /// Adds a <see cref="DependsOnClause{TEntity, TProperty}"/> to this <see cref="PropertyRule{TEntity, TProperty}"/> in that another specified property of the entity must have a non-default value (and not have a validation error) to continue.
        /// </summary>
        /// <param name="expression">A depends on expression.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public IPropertyRule<TEntity, TProperty> DependsOn<TDependsProperty>(Expression<Func<TEntity, TDependsProperty>> expression)
        {
            if (expression == null)
                return this;

            AddClause(new DependsOnClause<TEntity, TDependsProperty>(expression));
            return this;
        }
    }
}