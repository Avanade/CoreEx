// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation.Rules
{
    /// <summary>
    /// Provides a means to add an <see cref="EntityRule{TEntity, TProperty, TValidator}"/> using a validator <see cref="With"/> a specified validator <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="parent">The parent <see cref="IPropertyRule{TEntity, TProperty}"/>.</param>
    public class EntityRuleWith<TEntity, TProperty>(IPropertyRule<TEntity, TProperty> parent) where TEntity : class where TProperty : class?
    {
        private readonly IPropertyRule<TEntity, TProperty> _parent = parent.ThrowIfNull(nameof(parent));

        /// <summary>
        /// Adds an <see cref="EntityRule{TEntity, TProperty, TValidator}"/> using a validator <see cref="With"/> a specified <typeparamref name="TValidator"/>.
        /// </summary>
        /// <typeparam name="TValidator">The property validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/> where not specified.</param>
        /// <returns>A <see cref="PropertyRule{TEntity, TProperty}"/>.</returns>
        public IPropertyRule<TEntity, TProperty> With<TValidator>(IServiceProvider? serviceProvider = null) where TValidator : IValidatorEx
        {
            _parent.AddRule(new EntityRule<TEntity, TProperty, TValidator>(Validator.Create<TValidator>(serviceProvider)));
            return _parent;
        }
    }
}