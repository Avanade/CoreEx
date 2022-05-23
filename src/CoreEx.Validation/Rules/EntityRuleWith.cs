// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a means to add an <see cref="EntityRule{TEntity, TProperty, TValidator}"/> using a validator <see cref="With"/> a specified validator <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public class EntityRuleWith<TEntity, TProperty> where TEntity : class where TProperty : class?
    {
        private readonly PropertyRuleBase<TEntity, TProperty> _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityRuleWith{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="parent">The parent <see cref="PropertyRuleBase{TEntity, TProperty}"/>.</param>
        public EntityRuleWith(PropertyRuleBase<TEntity, TProperty> parent) => _parent = parent ?? throw new ArgumentNullException(nameof(parent));

        /// <summary>
        /// Adds an <see cref="EntityRule{TEntity, TProperty, TValidator}"/> using a validator <see cref="With"/> a specified <typeparamref name="TValidator"/>.
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/> where not specified.</param>
        /// <returns>A <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public PropertyRuleBase<TEntity, TProperty> With<TValidator>(IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx
        {
            _parent.AddRule(new EntityRule<TEntity, TProperty, TValidator>(Validator.Create<TValidator>(serviceProvider)));
            return _parent;
        }
    }
}