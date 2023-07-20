// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Results;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides entity validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public class Validator<TEntity> : ValidatorBase<TEntity> where TEntity : class
    {
        private RuleSet<TEntity>? _currentRuleSet;
        private Func<ValidationContext<TEntity>, CancellationToken, Task<Result>>? _additionalAsync;

        /// <inheritdoc/>
        public override Task<ValidationContext<TEntity>> ValidateAsync(TEntity value, ValidationArgs? args = null, CancellationToken cancellationToken = default)
        {
            return ValidationInvoker.Current.InvokeAsync(this, async cancellationToken =>
            {
                var context = new ValidationContext<TEntity>(value, args ?? new ValidationArgs());
                if (value is null)
                {
                    context.AddMessage(nameof(value), nameof(value), MessageType.Error, ValidatorStrings.MandatoryFormat, Validation.ValueTextDefault);
                    return context;
                }

                // Validate each of the property rules.
                foreach (var rule in Rules)
                {
                    await rule.ValidateAsync(context, cancellationToken).ConfigureAwait(false);

                    // Where in a failure state no further validation should be performed.
                    if (context.FailureResult.HasValue)
                        return context;
                }

                var result = await OnValidateAsync(context, cancellationToken).ConfigureAwait(false);
                if (result.IsSuccess && _additionalAsync != null)
                    result = await _additionalAsync(context, cancellationToken).ConfigureAwait(false);

                context.SetFailureResult(result);
                return context;
            }, cancellationToken);
        }

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added by the inheriting classes.
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext{TEntity}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="Result"/>.</returns>
        /// <remarks>The <paramref name="context"/> (see <see cref="ValidationContext{TEntity}"/> 'AddError' and related methods should be used for specific validation messages. Any <see cref="Result.IsFailure"/> <see cref="Result"/> will
        /// override any existing validations and no further validations will occur.</remarks>
        protected virtual Task<Result> OnValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken) => Task.FromResult(Result.Success);

        /// <summary>
        /// Adds a <see cref="PropertyRule{TEntity, TProperty}"/> to the validator.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public override IPropertyRule<TEntity, TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            // Depending on the the state update either the ruleset rules or the underlying rules.
            if (_currentRuleSet == null)
                return base.Property(propertyExpression);

            return _currentRuleSet.Property(propertyExpression);
        }

        /// <summary>
        /// Adds the <see cref="PropertyRule{TEntity, TProperty}"/> to the validator enabling additional configuration via the specified <paramref name="property"/> action.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="property">The action to act on the created <see cref="PropertyRule{TEntity, TProperty}"/>.</param>
        /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
        public Validator<TEntity> HasProperty<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, Action<IPropertyRule<TEntity, TProperty>>? property = null)
        {
            var p = Property(propertyExpression);
            property?.Invoke(p);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="IncludeBaseRule{TEntity, TInclude}"/> to the validator to enable a base validator to be included within the validator rule set.
        /// </summary>
        /// <typeparam name="TInclude">The include <see cref="Type"/> in which <typeparamref name="TEntity"/> inherits from.</typeparam>
        /// <param name="include">The <see cref="IValidatorEx{TInclude}"/> to include (add).</param>
        /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
        public Validator<TEntity> IncludeBase<TInclude>(IValidatorEx<TInclude> include) where TInclude : class
        {
            if (include == null)
                throw new ArgumentNullException(nameof(include));

            if (!typeof(TEntity).GetTypeInfo().IsSubclassOf(typeof(TInclude)))
                throw new ArgumentException($"Type {typeof(TEntity).Name} must inherit from {typeof(TInclude).Name}.");

            if (_currentRuleSet == null)
                base.Rules.Add(new IncludeBaseRule<TEntity, TInclude>(include));
            else
                _currentRuleSet.Rules.Add(new IncludeBaseRule<TEntity, TInclude>(include));

            return this;
        }

        /// <summary>
        /// Adds a <see cref="IncludeBaseRule{TEntity, TInclude}"/> to the validator to enable a base validator to be included within the validator rule.
        /// </summary>
        /// <typeparam name="TInclude">The include <see cref="Type"/> in which <typeparamref name="TEntity"/> inherits from.</typeparam>
        /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
        public Validator<TEntity> IncludeBase<TInclude>() where TInclude : class => IncludeBase(ExecutionContext.GetRequiredService<IValidatorEx<TInclude>>()!);

        /// <summary>
        /// Validate the entity value (post all configured property rules) enabling additional validation logic to be added.
        /// </summary>
        /// <param name="additionalAsync">The asynchronous function to invoke.</param>
        /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
        public Validator<TEntity> AdditionalAsync(Func<ValidationContext<TEntity>, CancellationToken, Task<Result>> additionalAsync)
        {
            if (_additionalAsync != null)
                throw new InvalidOperationException("Additional can only be defined once for a Validator.");

            _additionalAsync = additionalAsync ?? throw new ArgumentNullException(nameof(additionalAsync));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="RuleSet(Predicate{ValidationContext{TEntity}}, Action)"/> that is conditionally invoked where the <paramref name="predicate"/> is true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="action">The action to invoke where the <see cref="Property"/> method will update the corresponding <see cref="RuleSet(Predicate{ValidationContext{TEntity}}, Action)"/> <see cref="ValidatorBase{TEntity}.Rules">rules</see>.</param>
        /// <returns>The <see cref="RuleSet{TEntity}"/>.</returns>
        public RuleSet<TEntity> RuleSet(Predicate<ValidationContext<TEntity>> predicate, Action action)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return SetRuleSet(new RuleSet<TEntity>(predicate), (v) => action());
        }

        /// <summary>
        /// Adds a <see cref="RuleSet(Predicate{ValidationContext{TEntity}}, Action)"/> that is conditionally invoked where the <paramref name="predicate"/> is true.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <param name="action">The action to invoke where the passed <see cref="Validator{TEntity}"/> enables the <see cref="RuleSet(Predicate{ValidationContext{TEntity}}, Action)"/> <see cref="ValidatorBase{TEntity}.Rules">rules</see> to be updated.</param>
        /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
        public Validator<TEntity> HasRuleSet(Predicate<ValidationContext<TEntity>> predicate, Action<Validator<TEntity>> action)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            SetRuleSet(new RuleSet<TEntity>(predicate), action);
            return this;
        }

        /// <summary>
        /// Sets the rule set and invokes the action.
        /// </summary>
        private RuleSet<TEntity> SetRuleSet(RuleSet<TEntity> ruleSet, Action<Validator<TEntity>> action)
        {
            if (_currentRuleSet != null)
                throw new InvalidOperationException("RuleSets only support a single level of nesting.");

            // Invoke the action that will add the entries to the ruleset not the underlying rules.
            if (action != null)
            {
                _currentRuleSet = ruleSet;
                action(this);
                _currentRuleSet = null;
            }

            // Add the ruleset to the rules.
            Rules.Add(ruleSet);
            return ruleSet;
        }

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where the <see cref="MessageItem"/> <see cref="MessageItem.Property"/> is set based on the <paramref name="propertyExpression"/>.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="text">The message text.</param>
        public void ThrowValidationException<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, LText text)
        {
            var p = PropertyExpression.Create(propertyExpression);
            throw new ValidationException(MessageItem.CreateErrorMessage(ValidationArgs.DefaultUseJsonNames ? p.JsonName : p.Name, text));
        }

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where the <see cref="MessageItem"/> <see cref="MessageItem.Property"/> is set based on the <paramref name="propertyExpression"/>. The property
        /// friendly text and <paramref name="propertyValue"/> are automatically passed as the first two arguments to the string formatter.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <param name="format">The composite format string.</param>
        /// <param name="propertyValue">The property values (to be used as part of the format).</param>
        /// <param name="values"></param>
        public void ThrowValidationException<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression, LText format, TProperty propertyValue, params object[] values)
        {
            var p = PropertyExpression.Create(propertyExpression);
            throw new ValidationException(MessageItem.CreateErrorMessage(ValidationArgs.DefaultUseJsonNames ? p.JsonName : p.Name,
                string.Format(System.Globalization.CultureInfo.CurrentCulture, format, new object[] { p.Text, propertyValue! }.Concat(values).ToArray())));
        }
    }
}