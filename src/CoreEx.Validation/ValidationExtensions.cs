// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using CoreEx.Localization;
using CoreEx.RefData;
using CoreEx.Validation.Clauses;
using CoreEx.Validation.Rules;
using CoreEx.Wildcards;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides extension methods required by the validation framework (including support for fluent-style method chaining).
    /// </summary>
    public static class ValidationExtensions
    {
        #region Text

        /// <summary>
        /// Updates the rule friendly name text used in validation messages (see <see cref="IPropertyRule{TEntity, TProperty}.Text"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="text">The text for the rule.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Text<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText text) where TEntity : class
        {
            (rule ?? throw new ArgumentNullException(nameof(rule))).Text = text;
            return rule;
        }

        #endregion

        #region When

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> where the <typeparamref name="TEntity"/> <paramref name="predicate"/> must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="predicate">A function to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TEntity> predicate) where TEntity : class
        {
            if (predicate == null)
                return rule;

            (rule ?? throw new ArgumentNullException(nameof(rule))).AddClause(new WhenClause<TEntity, TProperty>(predicate));
            return rule;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> where the <typeparamref name="TProperty"/> <paramref name="predicate"/> must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="predicate">A function to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> WhenValue<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TProperty> predicate) where TEntity : class
        {
            if (predicate == null)
                return rule;

            (rule ?? throw new ArgumentNullException(nameof(rule))).AddClause(new WhenClause<TEntity, TProperty>(predicate));
            return rule;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> where the <typeparamref name="TProperty"/> must have a value (i.e. not the default value for the Type) for the rule to be validated.
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> WhenHasValue<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class
            => WhenValue(rule, (TProperty pv) => Comparer<TProperty>.Default.Compare(pv, default!) != 0);

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> which must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="when">A function to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<bool> when) where TEntity : class
        {
            if (when == null)
                return rule;

            (rule ?? throw new ArgumentNullException(nameof(rule))).AddClause(new WhenClause<TEntity, TProperty>(when));
            return rule;
        }

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> which must be <c>true</c> for the rule to be validated.
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="when">A <see cref="bool"/> to determine whether the preceeding rule is to be validated.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool when) where TEntity : class
            => When(rule, () => when);

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> that states that the
        /// <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext"/> <see cref="ExecutionContext.OperationType"/> is equal to the specified
        /// (<paramref name="operationType"/>).
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> WhenOperation<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, OperationType operationType) where TEntity : class
            => When(rule, x => ExecutionContext.Current.OperationType == operationType);

        /// <summary>
        /// Adds a <see cref="WhenClause{TEntity, TProperty}"/> to this <see cref="IPropertyRule{TEntity, TProperty}"/> that states that the
        /// <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext"/> <see cref="ExecutionContext.OperationType"/> is not equal to the specified
        /// (<paramref name="operationType"/>).
        /// </summary>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> WhenNotOperation<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, OperationType operationType) where TEntity : class
            => When(rule, x => ExecutionContext.Current.OperationType != operationType);

        #endregion

        #region Mandatory

        /// <summary>
        /// Adds a mandatory validation (<see cref="MandatoryRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Mandatory<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new MandatoryRule<TEntity, TProperty> { ErrorText = errorText });

        #endregion

        #region Must

        /// <summary>
        /// Adds a validation where the rule <paramref name="predicate"/> <b>must</b> return <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="predicate">The must predicate.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Must<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TEntity> predicate, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new MustRule<TEntity, TProperty>(predicate) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="must"/> function <b>must</b> return <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="must">The must function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Must<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<bool> must, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new MustRule<TEntity, TProperty>(must) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="must"/> value be <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="must">The must value.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Must<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool must, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new MustRule<TEntity, TProperty>(() => must) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="mustAsync"/> function <b>must</b> return <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="mustAsync">The must function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> MustAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<CancellationToken, Task<bool>> mustAsync, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new MustRule<TEntity, TProperty>(mustAsync) { ErrorText = errorText });

        #endregion

        #region Exists

        /// <summary>
        /// Adds a validation where the rule <paramref name="predicate"/> <b>exists</b> return <c>true</c> to verify it exists (see <see cref="ExistsRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="predicate">The exists predicate.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Exists<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TEntity> predicate, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ExistsRule<TEntity, TProperty>(predicate) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="exists"/> function <b>exists</b> return <c>true</c> to verify it exists (see <see cref="ExistsRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="exists">The exists function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> ExistsAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<bool>> exists, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ExistsRule<TEntity, TProperty>(exists) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="exists"/> value is <c>true</c> to verify it exists (see <see cref="ExistsRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="exists">The exists value.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Exists<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool exists, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ExistsRule<TEntity, TProperty>((_, __) => Task.FromResult(exists)) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="exists"/> function must return <b>not null</b> to verify it exists (see <see cref="ExistsRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="exists">The exists function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> ExistsAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<object?>> exists, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ExistsRule<TEntity, TProperty>(exists) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="exists"/> is <b>not null</b> to verify it exists (see <see cref="ExistsRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="exists">The exists function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Exists<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, object? exists, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ExistsRule<TEntity, TProperty>((_, __) => Task.FromResult(exists != null)) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="agentResult"/> function must return a successful response to verify it exists (see <see cref="ExistsRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="agentResult">The <see cref="HttpResult"/> function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> AgentExistsAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<HttpResult>> agentResult, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ExistsRule<TEntity, TProperty>(agentResult) { ErrorText = errorText });

        #endregion

        #region Duplicate

        /// <summary>
        /// Adds a validation where the rule <paramref name="predicate"/> <b>must</b> return <c>false</c> to not be considered a duplicate (see <see cref="DuplicateRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="predicate">The must predicate.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Duplicate<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TEntity> predicate, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DuplicateRule<TEntity, TProperty>(predicate) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="duplicate"/> function <b>must</b> return <c>false</c> to not be considered a duplicate (see <see cref="DuplicateRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="duplicate">The duplicate function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Duplicate<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<bool> duplicate, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DuplicateRule<TEntity, TProperty>(duplicate) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="duplicate"/> value must be <c>false</c> to not be considered a duplicate (see <see cref="DuplicateRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="duplicate">The duplicate value.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Duplicate<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool duplicate, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DuplicateRule<TEntity, TProperty>(() => duplicate) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where considered a duplicate (see <see cref="DuplicateRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Duplicate<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DuplicateRule<TEntity, TProperty>(() => true) { ErrorText = errorText });

        #endregion

        #region Immutable

        /// <summary>
        /// Adds a validation where the rule <paramref name="predicate"/> <b>must</b> return <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="predicate">The must predicate.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Immutable<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TEntity> predicate, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ImmutableRule<TEntity, TProperty>(predicate) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="immutable"/> function <b>must</b> return <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="immutable">The must function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Immutable<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<bool> immutable, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ImmutableRule<TEntity, TProperty>(immutable) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="immutableAsync"/> function <b>must</b> return <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="immutableAsync">The must function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> ImmutableAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<CancellationToken, Task<bool>> immutableAsync, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ImmutableRule<TEntity, TProperty>(immutableAsync) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where the rule <paramref name="immutable"/> value be <c>true</c> to be considered valid (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="immutable">The must value.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Immutable<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool immutable, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ImmutableRule<TEntity, TProperty>(() => immutable) { ErrorText = errorText });

        /// <summary>
        /// Adds a validation where considered immutable (see <see cref="MustRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Immutable<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ImmutableRule<TEntity, TProperty>(() => false) { ErrorText = errorText });

        #endregion

        #region Between

        /// <summary>
        /// Adds a between comparision validation against a specified from and to value (see <see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareFromValue">The compare from value.</param>
        /// <param name="compareToValue">The compare to value.</param>
        /// <param name="compareFromText">The compare from text to be passed for the error message (default is to use <paramref name="compareFromValue"/>).</param>
        /// <param name="compareToText">The compare to text to be passed for the error message (default is to use <paramref name="compareToValue"/>).</param>
        /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Between<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareFromValue, TProperty compareToValue, LText? compareFromText = null, LText? compareToText = null, bool exclusiveBetween = false, LText ? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new BetweenRule<TEntity, TProperty>(compareFromValue, compareToValue, compareFromText, compareToText, exclusiveBetween) { ErrorText = errorText });

        /// <summary>
        /// Adds a between comparision validation against from and to values returned by functions (<see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareFromValueFunction">The compare from value function.</param>
        /// <param name="compareToValueFunction">The compare to value function.</param>
        /// <param name="compareFromTextFunction">The compare from text function (default is to use the result of the <paramref name="compareFromValueFunction"/>).</param>
        /// <param name="compareToTextFunction">The compare to text function (default is to use the result of the <paramref name="compareToValueFunction"/>).</param>
        /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Between<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, TProperty> compareFromValueFunction, Func<TEntity, TProperty> compareToValueFunction, Func<TEntity, LText>? compareFromTextFunction = null, Func<TEntity, LText>? compareToTextFunction = null, bool exclusiveBetween = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new BetweenRule<TEntity, TProperty>(compareFromValueFunction, compareToValueFunction, compareFromTextFunction, compareToTextFunction, exclusiveBetween) { ErrorText = errorText });

        /// <summary>
        /// Adds a between comparision validation against from and to values returned by async functions (<see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareFromValueFunctionAsync">The compare from value function.</param>
        /// <param name="compareToValueFunctionAsync">The compare to value function.</param>
        /// <param name="compareFromTextFunction">The compare from text function (default is to use the result of the <paramref name="compareFromValueFunctionAsync"/>).</param>
        /// <param name="compareToTextFunction">The compare to text function (default is to use the result of the <paramref name="compareToValueFunctionAsync"/>).</param>
        /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> BetweenAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<TProperty>> compareFromValueFunctionAsync, Func<TEntity, CancellationToken, Task<TProperty>> compareToValueFunctionAsync, Func<TEntity, LText>? compareFromTextFunction = null, Func<TEntity, LText>? compareToTextFunction = null, bool exclusiveBetween = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new BetweenRule<TEntity, TProperty>(compareFromValueFunctionAsync, compareToValueFunctionAsync, compareFromTextFunction, compareToTextFunction, exclusiveBetween) { ErrorText = errorText });

        #endregion

        #region CompareValue

        /// <summary>
        /// Adds a comparision validation against a specified value (see <see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
        /// <param name="compareToValue">The compare to value.</param>
        /// <param name="compareToText">The compare to text <see cref="LText"/> to be passed for the error message (default is to use <paramref name="compareToValue"/>).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CompareValue<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, TProperty compareToValue, LText? compareToText = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CompareValueRule<TEntity, TProperty>(compareOperator, compareToValue, compareToText) { ErrorText = errorText });

        /// <summary>
        /// Adds a comparision validation against a value returned by a function (<see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
        /// <param name="compareToValueFunction">The compare to function.</param>
        /// <param name="compareToTextFunction">The compare to text function (default is to use the result of the <paramref name="compareToValueFunction"/>).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CompareValue<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, Func<TEntity, TProperty> compareToValueFunction, Func<TEntity, LText>? compareToTextFunction = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CompareValueRule<TEntity, TProperty>(compareOperator, compareToValueFunction, compareToTextFunction) { ErrorText = errorText });

        /// <summary>
        /// Adds a comparision validation against a value returned by an async function (<see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
        /// <param name="compareToValueFunctionAsync">The compare to function.</param>
        /// <param name="compareToTextFunction">The compare to text function (default is to use the result of the <paramref name="compareToValueFunctionAsync"/>).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CompareValueAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, Func<TEntity, CancellationToken, Task<TProperty>> compareToValueFunctionAsync, Func<TEntity, LText>? compareToTextFunction = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CompareValueRule<TEntity, TProperty>(compareOperator, compareToValueFunctionAsync, compareToTextFunction) { ErrorText = errorText });

        /// <summary>
        /// Adds a comparision validation against one or more specified values (see <see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareToValues">The compare to values.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CompareValues<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, IEnumerable<TProperty> compareToValues, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CompareValuesRule<TEntity, TProperty>(compareToValues) { ErrorText = errorText });

        /// <summary>
        /// Adds a comparision validation against one or more specified values (see <see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareToValuesFunctionAsync">The compare to values function.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CompareValues<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<IEnumerable<TProperty>>> compareToValuesFunctionAsync, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CompareValuesRule<TEntity, TProperty>(compareToValuesFunctionAsync) { ErrorText = errorText });

        /// <summary>
        /// Adds a comparision validation against one or more specified values (see <see cref="CompareValueRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareToValues">The compare to values.</param>
        /// <param name="ignoreCase">Indicates whether to ignore the casing of the value when comparing.</param>
        /// <param name="overrideValue">Indicates whether to override the underlying property value with the corresponding matched value.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, string> CompareValues<TEntity>(this IPropertyRule<TEntity, string> rule, IEnumerable<string> compareToValues, bool ignoreCase, bool overrideValue = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CompareValuesRule<TEntity, string>(compareToValues) { EqualityComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, OverrideValue = overrideValue, ErrorText = errorText });

        #endregion

        #region CompareProperty

        /// <summary>
        /// Adds a comparision validation against a specified property (see <see cref="ComparePropertyRule{TEntity, TProperty, TProperty2}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <typeparam name="TCompareProperty">The compare to property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
        /// <param name="compareToPropertyExpression">The <see cref="Expression"/> to reference the compare to entity property.</param>
        /// <param name="compareToText">The compare to text <see cref="LText"/> to be passed for the error message (default is to use <paramref name="compareToPropertyExpression"/>).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CompareProperty<TEntity, TProperty, TCompareProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, Expression<Func<TEntity, TCompareProperty>> compareToPropertyExpression, LText? compareToText = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ComparePropertyRule<TEntity, TProperty, TCompareProperty>(compareOperator, compareToPropertyExpression, compareToText) { ErrorText = errorText });

        #endregion

        #region String

#pragma warning disable CA1720 // Identifier contains type name; by-design, best name.

        /// <summary>
        /// Adds a <see cref="string"/> validation with a maximum length (see <see cref="StringRule{TEntity}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, String}"/> being extended.</param>
        /// <param name="maxLength">The maximum string length.</param>
        /// <param name="regex">The <see cref="Regex"/>.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, int maxLength, Regex? regex = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new StringRule<TEntity> { MaxLength = maxLength, Regex = regex, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="string"/> validation with a minimum and maximum length (see <see cref="StringRule{TEntity}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, String}"/> being extended.</param>
        /// <param name="minLength">The minimum string length.</param>
        /// <param name="maxLength">The maximum string length.</param>
        /// <param name="regex">The <see cref="Regex"/>.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, int minLength, int? maxLength, Regex? regex = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new StringRule<TEntity> { MinLength = minLength, MaxLength = maxLength, Regex = regex, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="string"/> validation with a <paramref name="regex"/> (see <see cref="StringRule{TEntity}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, String}"/> being extended.</param>
        /// <param name="regex">The <see cref="Regex"/>.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, Regex? regex = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new StringRule<TEntity> { Regex = regex, ErrorText = errorText });
#pragma warning restore CA1720

        #endregion

        #region Email

        /// <summary>
        /// Adds an e-mail validation (see <see cref="EmailRule{TEntity}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, String}"/> being extended.</param>
        /// <param name="maxLength">The maximum string length for the e-mail address; defaults to 254.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        /// <remarks>The maximum length for an email address is '<c>254</c>' as per this <see href="https://stackoverflow.com/questions/386294/what-is-the-maximum-length-of-a-valid-email-address#:~:text=%20The%20length%20limits%20are%20as%20follows%3A%20,i.e.%2C%20example.com%20--%20254%20characters%20maximum.%20More%20">article</see>,
        /// hence the default.</remarks>
        public static IPropertyRule<TEntity, string> Email<TEntity>(this IPropertyRule<TEntity, string> rule, int? maxLength = 254, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new EmailRule<TEntity> { MaxLength = maxLength, ErrorText = errorText });

        #endregion

        #region Enum

        /// <summary>
        /// Adds an <see cref="System.Enum"/> validation to ensure that the value has been defined (see <see cref="EnumRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText? errorText = null) where TEntity : class where TProperty : struct, Enum
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new EnumRule<TEntity, TProperty> { ErrorText = errorText });

        /// <summary>
        /// Adds an <see cref="System.Enum"/> validation to ensure that the value has been defined (see <see cref="NullableEnumRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty?> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, LText? errorText = null) where TEntity : class where TProperty : struct, Enum
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new NullableEnumRule<TEntity, TProperty> { ErrorText = errorText });

        /// <summary>
        /// Enables the addition of an <see cref="EnumValueRule{TEntity, TEnum}"/> using an <see cref="EnumValueRuleAs{TEntity}.As{TEnum}"/> to validate against a specified <see cref="System.Enum"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static EnumValueRuleAs<TEntity> Enum<TEntity>(this IPropertyRule<TEntity, string> rule) where TEntity : class
            => new(rule ?? throw new ArgumentNullException(nameof(rule))) { };

        #endregion

        #region Wildcard

        /// <summary>
        /// Adds a <see cref="string"/> <see cref="Wildcard"/> validation (see <see cref="WildcardRule{TEntity}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, String}"/> being extended.</param>
        /// <param name="wildcard">The <see cref="Wildcard"/> configuration (defaults to <see cref="Wildcard.Default"/>).</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, String}"/>.</returns>
        public static IPropertyRule<TEntity, string> Wildcard<TEntity>(this IPropertyRule<TEntity, string> rule, Wildcard? wildcard = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new WildcardRule<TEntity> { Wildcard = wildcard, ErrorText = errorText });

        #endregion

        #region Numeric

        /// <summary>
        /// Adds a <see cref="Int32"/> validation (see <see cref="DecimalRule{TEntity, Int32}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Int32}"/> being extended.</param> 
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Int32}"/>.</returns>
        public static IPropertyRule<TEntity, int> Numeric<TEntity>(this IPropertyRule<TEntity, int> rule, bool allowNegatives = false, int? maxDigits = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, int> { AllowNegatives = allowNegatives, MaxDigits = maxDigits, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Nullable{Int32}"/> validation (see <see cref="DecimalRule{TEntity, Int32}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Int32}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Int32}"/>.</returns>
        public static IPropertyRule<TEntity, int?> Numeric<TEntity>(this IPropertyRule<TEntity, int?> rule, bool allowNegatives = false, int? maxDigits = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, int?> { AllowNegatives = allowNegatives, MaxDigits = maxDigits, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Int64"/> validation (see <see cref="DecimalRule{TEntity, Int64}"/>);
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Long}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Int64}"/>.</returns>
        public static IPropertyRule<TEntity, long> Numeric<TEntity>(this IPropertyRule<TEntity, long> rule, bool allowNegatives = false, int? maxDigits = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, long> { AllowNegatives = allowNegatives, MaxDigits = maxDigits, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Nullable{Int64}"/> validation (see <see cref="DecimalRule{TEntity, Int64}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Int64}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Int64}"/>.</returns>
        public static IPropertyRule<TEntity, long?> Numeric<TEntity>(this IPropertyRule<TEntity, long?> rule, bool allowNegatives = false, int? maxDigits = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, long?> { AllowNegatives = allowNegatives, MaxDigits = maxDigits, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Decimal"/> validation (see <see cref="DecimalRule{TEntity, Decimal}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Decimal}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits (including decimal places).</param>
        /// <param name="decimalPlaces">The maximum number of decimal places.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Decimal}"/>.</returns>
        public static IPropertyRule<TEntity, decimal> Numeric<TEntity>(this IPropertyRule<TEntity, decimal> rule, bool allowNegatives = false, int? maxDigits = null, int? decimalPlaces = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, decimal> { AllowNegatives = allowNegatives, MaxDigits = maxDigits, DecimalPlaces = decimalPlaces, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Nullable{Decimal}"/> validation (see <see cref="DecimalRule{TEntity, Decimal}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Decimal}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits (including decimal places).</param>
        /// <param name="decimalPlaces">The maximum number of decimal places.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Decimal}"/>.</returns>
        public static IPropertyRule<TEntity, decimal?> Numeric<TEntity>(this IPropertyRule<TEntity, decimal?> rule, bool allowNegatives = false, int? maxDigits = null, int? decimalPlaces = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, decimal?> { AllowNegatives = allowNegatives, MaxDigits = maxDigits, DecimalPlaces = decimalPlaces, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Single"/> validation (see <see cref="NumericRule{TEntity, Single}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Single}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Single}"/>.</returns>
        public static IPropertyRule<TEntity, float> Numeric<TEntity>(this IPropertyRule<TEntity, float> rule, bool allowNegatives = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new NumericRule<TEntity, float> { AllowNegatives = allowNegatives, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Nullable{Single}"/> validation (see <see cref="NumericRule{TEntity, Single}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Single}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Single}"/>.</returns>
        public static IPropertyRule<TEntity, float?> Numeric<TEntity>(this IPropertyRule<TEntity, float?> rule, bool allowNegatives = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new NumericRule<TEntity, float?> { AllowNegatives = allowNegatives, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Double"/> validation (see <see cref="NumericRule{TEntity, Double}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Double}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Double}"/>.</returns>
        public static IPropertyRule<TEntity, double> Numeric<TEntity>(this IPropertyRule<TEntity, double> rule, bool allowNegatives = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new NumericRule<TEntity, double> { AllowNegatives = allowNegatives, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="Nullable{Double}"/> validation (see <see cref="NumericRule{TEntity, Double}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Double}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Double}"/>.</returns>
        public static IPropertyRule<TEntity, double?> Numeric<TEntity>(this IPropertyRule<TEntity, double?> rule, bool allowNegatives = false, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new NumericRule<TEntity, double?> { AllowNegatives = allowNegatives, ErrorText = errorText });

        #endregion

        #region Currency

        /// <summary>
        /// Adds a currency (<see cref="Decimal"/>) validation (see <see cref="DecimalRule{TEntity, Decimal}"/> for an <see cref="Decimal"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Decimal}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits (including decimal places).</param>
        /// <param name="currencyFormatInfo">The <see cref="NumberFormatInfo"/> that the <see cref="NumberFormatInfo.CurrencyDecimalDigits">decimal places</see> will be derived from;
        /// where <c>null</c> <see cref="NumberFormatInfo.CurrentInfo"/> will be used as a default.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Decimal}"/>.</returns>
        public static IPropertyRule<TEntity, decimal> Currency<TEntity>(this IPropertyRule<TEntity, decimal> rule, bool allowNegatives = false, int? maxDigits = null, NumberFormatInfo? currencyFormatInfo = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, decimal>
            {
                AllowNegatives = allowNegatives,
                MaxDigits = maxDigits,
                DecimalPlaces = currencyFormatInfo == null ? NumberFormatInfo.CurrentInfo.CurrencyDecimalDigits : currencyFormatInfo.CurrencyDecimalDigits,
                ErrorText = errorText
            });

        /// <summary>
        /// Adds a currency <see cref="Nullable{Decimal}"/> validation (see <see cref="DecimalRule{TEntity, Decimal}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, Decimal}"/> being extended.</param>
        /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
        /// <param name="maxDigits">The maximum digits (including decimal places).</param>
        /// <param name="currencyFormatInfo">The <see cref="NumberFormatInfo"/> that the <see cref="NumberFormatInfo.CurrencyDecimalDigits">decimal places</see> will be derived from;
        /// where <c>null</c> <see cref="NumberFormatInfo.CurrentInfo"/> will be used as a default.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, Decimal}"/>.</returns>
        public static IPropertyRule<TEntity, decimal?> Currency<TEntity>(this IPropertyRule<TEntity, decimal?> rule, bool allowNegatives = false, int? maxDigits = null, NumberFormatInfo? currencyFormatInfo = null, LText? errorText = null) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new DecimalRule<TEntity, decimal?>
            {
                AllowNegatives = allowNegatives,
                MaxDigits = maxDigits,
                DecimalPlaces = currencyFormatInfo == null ? NumberFormatInfo.CurrentInfo.CurrencyDecimalDigits : currencyFormatInfo.CurrencyDecimalDigits,
                ErrorText = errorText
            });

        #endregion

        #region ReferenceData

        /// <summary>
        /// Adds a <see cref="IReferenceData"/> validation (see <see cref="ReferenceDataRule{TEntity, TProperty}"/>) to ensure the value is valid.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/> (must inherit from <see cref="IReferenceData"/>).</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> IsValid<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText? errorText = null) where TEntity : class where TProperty : IReferenceData?
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ReferenceDataRule<TEntity, TProperty> { ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="IReferenceDataCodeList"/> validation (see <see cref="ReferenceDataRule{TEntity, TProperty}"/>) to ensure the list of SIDs are valid.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/> (must inherit from <see cref="IReferenceDataCodeList"/>).</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="allowDuplicates">Indicates whether duplicate values are allowed.</param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty?> AreValid<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, bool allowDuplicates = false, int minCount = 0, int? maxCount = null, LText? errorText = null) where TEntity : class where TProperty : IReferenceDataCodeList
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new ReferenceDataSidListRule<TEntity, TProperty> { AllowDuplicates = allowDuplicates, MinCount = minCount, MaxCount = maxCount, ErrorText = errorText });

        /// <summary>
        /// Adds a <see cref="IReferenceData.Code"/> validation (see <see cref="ReferenceDataCodeRule{TEntity, TRefData}"/> to ensure the <c>Code</c> is valid.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="errorText">The error message format text <see cref="LText"/> (overrides the default).</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static ReferenceDataCodeRuleAs<TEntity> RefDataCode<TEntity>(this IPropertyRule<TEntity, string> rule, LText? errorText = null) where TEntity : class
            => new(rule ?? throw new ArgumentNullException(nameof(rule)), errorText);

        #endregion

        #region Collection

        /// <summary>
        /// Adds a collection (<see cref="System.Collections.IEnumerable"/>) validation (see <see cref="CollectionRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="item">The item <see cref="ICollectionRuleItem"/> configuration.</param>
        /// <param name="allowNullItems">Indicates whether the underlying collection item must not be null.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Collection<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, int minCount = 0, int? maxCount = null, ICollectionRuleItem? item = null, bool allowNullItems = false) where TEntity : class where TProperty : System.Collections.IEnumerable?
        {
            var cr = new CollectionRule<TEntity, TProperty> { MinCount = minCount, MaxCount = maxCount, Item = item, AllowNullItems = allowNullItems };
            return (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(cr);
        }

        #endregion

        #region Dictionary

        /// <summary>
        /// Adds a dictionary (<see cref="System.Collections.IDictionary"/>) validation (see <see cref="DictionaryRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="minCount">The minimum count.</param>
        /// <param name="maxCount">The maximum count.</param>
        /// <param name="item">The item <see cref="IDictionaryRuleItem"/> configuration.</param>
        /// <param name="allowNullKeys">Indicates whether the underlying dictionary keys must not be null.</param>
        /// <param name="allowNullValues">Indicates whether the underlying dictionary values must not be null.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Dictionary<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, int minCount = 0, int? maxCount = null, IDictionaryRuleItem? item = null, bool allowNullKeys = false, bool allowNullValues = false) where TEntity : class where TProperty : System.Collections.IDictionary?
        {
            var cr = new DictionaryRule<TEntity, TProperty> { MinCount = minCount, MaxCount = maxCount, Item = item, AllowNullKeys = allowNullKeys, AllowNullValues = allowNullValues };
            return (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(cr);
        }

        #endregion

        #region Entity

        /// <summary>
        /// Adds an entity validation (see <see cref="EntityRule{TEntity, TProperty, TValidator}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="validator">The validator.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Entity<TEntity, TProperty, TValidator>(this IPropertyRule<TEntity, TProperty> rule, TValidator validator) where TEntity : class where TProperty : class? where TValidator : IValidatorEx
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new EntityRule<TEntity, TProperty, TValidator>(validator));

        /// <summary>
        /// Enables the addition of an <see cref="EntityRule{TEntity, TProperty, TValidator}"/> using a validator <see cref="EntityRuleWith{TEntity, TProperty}.With{TValidator}"/> a specified validator <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <returns>An <see cref="EntityRuleWith{TEntity, TProperty}"/>.</returns>
        public static EntityRuleWith<TEntity, TProperty> Entity<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class where TProperty : class?
            => new(rule ?? throw new ArgumentNullException(nameof(rule)));

        #endregion

        #region Custom

        /// <summary>
        /// Adds a <paramref name="custom"/> validation (see <see cref="CustomRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="custom">The custom action.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Custom<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Action<PropertyContext<TEntity, TProperty>> custom) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CustomRule<TEntity, TProperty>(custom));

        /// <summary>
        /// Adds a <paramref name="customAsync"/> validation (see <see cref="CustomRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="customAsync">The custom function.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> CustomAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, CancellationToken, Task> customAsync) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CustomRule<TEntity, TProperty>(customAsync));

        #endregion

        #region Common

        /// <summary>
        /// Adds a common validation (see <see cref="CommonRule{TEntity, TProperty}"/>).
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
        /// <param name="validator">The <see cref="CommonValidator{T}"/>.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Common<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, CommonValidator<TProperty> validator) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new CommonRule<TEntity, TProperty>(validator));

        #endregion

        #region Override/Default

        /// <summary>
        /// Adds a value override (see <see cref="OverrideRule{TEntity, TProperty}"/>) using the specified <paramref name="overrideFunc"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param> 
        /// <param name="overrideFunc">The override function.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Override<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, TProperty> overrideFunc) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new OverrideRule<TEntity, TProperty>(overrideFunc));

        /// <summary>
        /// Adds a value override (see <see cref="OverrideRule{TEntity, TProperty}"/>) using the specified <paramref name="overrideFuncAsync"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param> 
        /// <param name="overrideFuncAsync">The override function.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> OverrideAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<TProperty>> overrideFuncAsync) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new OverrideRule<TEntity, TProperty>(overrideFuncAsync));

        /// <summary>
        /// Adds a value override (see <see cref="OverrideRule{TEntity, TProperty}"/>) using the specified <paramref name="overrideValue"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param> 
        /// <param name="overrideValue">The override value.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Override<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty overrideValue) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new OverrideRule<TEntity, TProperty>(overrideValue));

        /// <summary>
        /// Adds a default (see <see cref="OverrideRule{TEntity, TProperty}"/>) using the specified <paramref name="defaultFunc"/> (overrides only where current value is the default for <see cref="Type"/>) .
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param> 
        /// <param name="defaultFunc">The override function.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Default<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, TProperty> defaultFunc) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new OverrideRule<TEntity, TProperty>(defaultFunc) { OnlyOverrideDefault = true });

        /// <summary>
        /// Adds a default (see <see cref="OverrideRule{TEntity, TProperty}"/>) using the specified <paramref name="defaultFuncAsync"/> (overrides only where current value is the default for <see cref="Type"/>) .
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param> 
        /// <param name="defaultFuncAsync">The override function.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> DefaultAsync<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<TEntity, CancellationToken, Task<TProperty>> defaultFuncAsync) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new OverrideRule<TEntity, TProperty>(defaultFuncAsync) { OnlyOverrideDefault = true });

        /// <summary>
        /// Adds a default override (see <see cref="OverrideRule{TEntity, TProperty}"/>) using the specified <paramref name="defaultValue"/> (overrides only where current value is the default for <see cref="Type"/>) .
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param> 
        /// <param name="defaultValue">The override value.</param>
        /// <returns>A <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
        public static IPropertyRule<TEntity, TProperty> Default<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty defaultValue) where TEntity : class
            => (rule ?? throw new ArgumentNullException(nameof(rule))).AddRule(new OverrideRule<TEntity, TProperty>(defaultValue) { OnlyOverrideDefault = true });

        #endregion

        #region ValueValidator

        /// <summary>
        /// Enables (sets up) validation for a value.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="name">The value name (defaults to <see cref="Validation.ValueNameDefault"/>).</param>
        /// <param name="text">The friendly text name used in validation messages (defaults to <paramref name="name"/> as sentence case where not specified).</param>
        /// <returns>A <see cref="ValueValidator{T}"/>.</returns>
        public static ValueValidator<T?> Validate<T>(this T? value, string? name = null, LText? text = null) => new(value, name, text);

        #endregion

        #region MultiValidator

        /// <summary>
        /// Adds a <see cref="ValueValidator{T}"/> to the <see cref="MultiValidator"/>. 
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="multiValidator">The <see cref="MultiValidator"/>.</param>
        /// <param name="validator">The <see cref="ValueValidator{T}"/>.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public static MultiValidator Add<T>(this MultiValidator multiValidator, ValueValidator<T?> validator)
        {
            if (multiValidator == null)
                throw new ArgumentNullException(nameof(multiValidator));

            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            (multiValidator ?? throw new ArgumentNullException(nameof(multiValidator))).Validators.Add(async ct => await validator.ValidateAsync(ct).ConfigureAwait(false));
            return multiValidator;
        }

        /// <summary>
        /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the <see cref="MultiValidator"/>. 
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="multiValidator">The <see cref="MultiValidator"/>.</param>
        /// <param name="validator">The <see cref="PropertyRuleBase{TEntity, TProperty}"/> <see cref="ValueValidator{T}"/>.</param>
        /// <returns>The (this) <see cref="MultiValidator"/>.</returns>
        public static MultiValidator Add<T>(this MultiValidator multiValidator, IPropertyRule<ValidationValue<T>, T> validator)
        {
            if (multiValidator == null)
                throw new ArgumentNullException(nameof(multiValidator));

            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            (multiValidator ?? throw new ArgumentNullException(nameof(multiValidator))).Validators.Add(validator.ValidateAsync);
            return multiValidator;
        }

        #endregion
    }
}