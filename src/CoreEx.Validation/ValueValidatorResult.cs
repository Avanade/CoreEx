// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Linq;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables the result of a <see cref="ValueValidator{T}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public interface IValueValidatorResult<TEntity, out TProperty> : IValidationResult<TProperty> where TEntity : class { }

    /// <summary>
    /// Represents the result of a <see cref="ValueValidator{T}"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public sealed class ValueValidatorResult<TEntity, TProperty> : IValueValidatorResult<TEntity, TProperty> where TEntity : class
    {
        private readonly PropertyContext<TEntity, TProperty> _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueValidatorResult{TEntity, TProperty}"/> class.
        /// </summary>
        /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
        public ValueValidatorResult(PropertyContext<TEntity, TProperty> context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        /// <inheritdoc/>
        public TProperty? Value => _context.Value;

        /// <inheritdoc/>
        public bool HasErrors => _context.HasError; 

        /// <inheritdoc/>
        public MessageItemCollection? Messages => _context.Parent.Messages;

        /// <inheritdoc/>
        public ValidationException? ToValidationException() => HasErrors ? new ValidationException(Messages!) : null;

        /// <inheritdoc/>
        IValidationResult IValidationResult.ThrowOnError() => ThrowOnError();

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where an error was found (and optionally if warnings).
        /// </summary>
        /// <param name="includeWarnings">Indicates whether to throw where only warnings exist.</param>
        /// <returns>The <see cref="ValidationContext{TEntity}"/> to support fluent-style method-chaining.</returns>
        public ValueValidatorResult<TEntity, TProperty> ThrowOnError(bool includeWarnings = false) => (HasErrors || (includeWarnings && Messages != null && Messages.Any(x => x.Type == MessageType.Warning))) ? throw ToValidationException()! : this;
    }
}