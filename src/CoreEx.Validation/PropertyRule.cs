// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Validation.Clauses;
using CoreEx.Validation.Rules;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents a validation rule for an entity property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class PropertyRule<TEntity, TProperty> : PropertyRuleBase<TEntity, TProperty>, IPropertyRule<TEntity>, IValueRule<TEntity, TProperty> where TEntity : class
    {
        private readonly PropertyExpression<TEntity, TProperty> _property;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRule{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="propertyExpression">The <see cref="LambdaExpression"/> to reference the entity property.</param>
        public PropertyRule(Expression<Func<TEntity, TProperty>> propertyExpression) : this(PropertyExpression.Create(propertyExpression)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRule{TEntity, TProperty}"/> class.
        /// </summary>
        private PropertyRule(PropertyExpression<TEntity, TProperty> propertyExpression) : base(propertyExpression.Name, propertyExpression.Text, propertyExpression.JsonName) => _property = propertyExpression;

        /// <inheritdoc/>
        public async Task ValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Value == null)
                return;

            // Where validating a specific property then make sure the names match.
            if (context.SelectedPropertyName != null && context.SelectedPropertyName != Name)
                return;

            // Ensure that the property does not already have an error.
            if (context.HasError(_property))
                return;

            // Get the property value and create the property context.
            var value = _property.GetValue(context.Value);
            var ctx = new PropertyContext<TEntity, TProperty>(context, value, this.Name, this.JsonName, this.Text);

            // Run the rules.
            await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a rule (<see cref="IValueRule{TEntity, TProperty}"/>) to the property.
        /// </summary>
        /// <param name="rule">The <see cref="IValueRule{TEntity, TProperty}"/>.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public new PropertyRule<TEntity, TProperty> AddRule(IValueRule<TEntity, TProperty> rule)
        {
            base.AddRule(rule);
            return this;
        }

        /// <inheritdoc/>
        bool IValueRule<TEntity, TProperty>.Check(IPropertyContext context) => throw new NotSupportedException("A property value clauses check should not occur directly on a PropertyRule.");

        /// <inheritdoc/>
        Task IValueRule<TEntity, TProperty>.ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken) => throw new NotSupportedException("A property value validation should not occur directly on a PropertyRule.");

        /// <inheritdoc/>
        void IValueRule<TEntity, TProperty>.AddClause(IPropertyRuleClause<TEntity> clause) => AddClause(clause);

        /// <inheritdoc/>
        public override Task<ValueValidatorResult<TEntity, TProperty>> RunAsync(bool throwOnError = false, CancellationToken cancellationToken = default) => throw new NotSupportedException("The RunAsync method is not supported for a PropertyRule<TEntity, TProperty>.");
    }
}