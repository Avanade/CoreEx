// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the base entity validator.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public abstract class ValidatorBase<TEntity> : IValidatorEx<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets the underlying rules collection.
        /// </summary>
        internal protected List<IPropertyRule<TEntity>> Rules { get; } = new List<IPropertyRule<TEntity>>();

        /// <summary>
        /// Gets the <see cref="ExecutionContext.Current"/> instance.
        /// </summary>
        public CoreEx.ExecutionContext ExecutionContext => ExecutionContext.Current;

        /// <summary>
        /// Adds a <see cref="PropertyRule{TEntity, TProperty}"/> to the validator.
        /// </summary>
        /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
        /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
        /// <returns>The <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public virtual IPropertyRule<TEntity, TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression)
        {
            PropertyRule<TEntity, TProperty> rule = new(propertyExpression);
            Rules.Add(rule);
            return rule;
        }

        /// <inheritdoc/>
        public virtual Task<ValidationContext<TEntity>> ValidateAsync(TEntity value, ValidationArgs? args, CancellationToken cancellationToken = default) => throw new NotSupportedException("Validate is not supported by this class.");
    }
}