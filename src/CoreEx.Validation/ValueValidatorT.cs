// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables validation for a single entity property value (using the property to determine the error message text).
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class ValueValidator<TEntity, TProperty> : PropertyRule<TEntity, TProperty> where TEntity : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueValidator{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="propertyExpression">The property expression.</param>
        /// <param name="value">The value to validate.</param>
        public ValueValidator(Expression<Func<TEntity, TProperty>> propertyExpression, TProperty value) : base(propertyExpression) =>  Value = value;

        /// <summary>
        /// Gets the value.
        /// </summary>
        public TProperty Value { get; }

        /// <inheritdoc/>
        public override async Task<ValueValidatorResult<TEntity, TProperty>> RunAsync(bool throwOnError = false, CancellationToken cancellationToken = default)
        {
            var ctx = new PropertyContext<TEntity, TProperty>(new ValidationContext<TEntity>(null!, new ValidationArgs()), Value, this.Name, this.JsonName, this.Text);
            await InvokeAsync(ctx, cancellationToken).ConfigureAwait(false);
            var res = new ValueValidatorResult<TEntity, TProperty>(ctx);
            if (throwOnError)
                res.ThrowOnError();

            return res;
        }
    }
}