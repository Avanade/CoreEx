// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides a validation rule for an entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    public interface IEntityRule<TEntity> where TEntity : class
    {
        /// <summary>
        /// Validates an entity given a <see cref="ValidationContext{TEntity}"/>.
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext{TEntity}"/></param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task ValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken);
    }
}