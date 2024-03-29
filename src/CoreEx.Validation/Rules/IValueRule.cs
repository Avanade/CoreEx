// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using CoreEx.Validation.Clauses;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides the rule to <see cref="ValidateAsync">validate</see> a value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TProperty">The value <see cref="System.Type"/>.</typeparam>
    public interface IValueRule<TEntity, out TProperty> where TEntity : class
    {
        /// <summary>
        /// Gets or sets the error message format text (overrides the default).
        /// </summary>
        LText? ErrorText { get; set; }

        /// <summary>
        /// Adds a <see cref="IPropertyRuleClause{TEntity}"/>.
        /// </summary>
        /// <param name="clause">The <see cref="IPropertyRuleClause{TEntity}"/>.</param>
        void AddClause(IPropertyRuleClause<TEntity> clause);

        /// <summary>
        /// Checks the clauses.
        /// </summary>
        /// <param name="context">The <see cref="IPropertyContext"/>.</param>
        /// <returns><c>true</c> where validation is to continue; otherwise, <c>false</c> to stop.</returns>
        bool Check(IPropertyContext context);

        /// <summary>
        /// Validate the value.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task ValidateAsync(IPropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken);
    }
}