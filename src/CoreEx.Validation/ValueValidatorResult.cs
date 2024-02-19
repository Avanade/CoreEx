// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;

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
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    public sealed class ValueValidatorResult<TEntity, TProperty>(PropertyContext<TEntity, TProperty> context) : IValueValidatorResult<TEntity, TProperty> where TEntity : class
    {
        private readonly PropertyContext<TEntity, TProperty> _context = context.ThrowIfNull(nameof(context));

        /// <inheritdoc/>
        public TProperty? Value => _context.Value;

        /// <inheritdoc/>
        public bool HasErrors => _context.Parent.FailureResult is not null || _context.HasError; 

        /// <inheritdoc/>
        public MessageItemCollection? Messages => _context.Parent.Messages;

        /// <inheritdoc/>
        public Exception? ToException() => _context.Parent.ToException();

        /// <inheritdoc/>
        IValidationResult IValidationResult.ThrowOnError() => ThrowOnError();

        /// <inheritdoc/>
        public Result? FailureResult => _context.Parent.FailureResult;

        /// <inheritdoc/>
        public Result<R> ToResult<R>() => FailureResult.HasValue ? FailureResult.Value.Bind<R>() : (HasErrors ? Result<R>.ValidationError(Messages!) : Validation.ConvertValueToResult<TProperty, R>(Value!));

        /// <inheritdoc/>
        public Result ToResult() => FailureResult ?? (HasErrors ? Result.ValidationError(Messages!) : Result.Success);

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where an error was found (and optionally if warnings).
        /// </summary>
        /// <param name="includeWarnings">Indicates whether to throw where only warnings exist.</param>
        /// <returns>The <see cref="ValidationContext{TEntity}"/> to support fluent-style method-chaining.</returns>
        public ValueValidatorResult<TEntity, TProperty> ThrowOnError(bool includeWarnings = false)
        {
            _context.Parent.ThrowOnError(includeWarnings);
            return this;
        }
    }
}