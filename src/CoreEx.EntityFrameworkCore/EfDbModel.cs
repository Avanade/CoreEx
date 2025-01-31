// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides a lightweight typed <b>Entity Framework</b> wrapper over the <see cref="IEfDb"/> operations that are <typeparamref name="TModel"/>-specific.
    /// </summary>
    /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
    /// <param name="efDb">The <see cref="IEfDb"/>.</param>
    public readonly struct EfDbModel<TModel>(IEfDb efDb) where TModel : class, new()
    {
        /// <inheritdoc/>
        public IEfDb EfDb { get; } = efDb.ThrowIfNull(nameof(efDb));

        /// <summary>
        /// Creates an <see cref="EfDbQuery{TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{TModel}"/>.</returns>
        public EfDbQuery<TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Query(new EfDbArgs(EfDb.DbArgs), query);

        /// <summary>
        /// Creates an <see cref="EfDbQuery{TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <param name="queryArgs">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{TModel}"/>.</returns>
        public EfDbQuery<TModel> Query(EfDbArgs queryArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => new(EfDb, queryArgs, query);

        #region Standard

        /// <summary>
        /// Gets the model for the specified <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<TModel?> GetAsync(params object?[] keys) => GetAsync(keys, default);

        /// <summary>
        /// Gets the model for the specified <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<TModel?> GetAsync(object?[] keys, CancellationToken cancellationToken = default) => GetAsync(CompositeKey.Create(keys), cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then <c>null</c> will be returned.</remarks>
        public Task<TModel?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetAsync(new EfDbArgs(EfDb.DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the model for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then <c>null</c> will be returned.</remarks>
        public Task<TModel?> GetAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default)
            => EfDb.GetAsync<TModel>(args, key, cancellationToken);

        #endregion

        #region WithResult

        /// <summary>
        /// Gets the model for the specified <paramref name="keys"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <returns>The model value where found; otherwise, <c>null</c>.</returns>
        public Task<Result<TModel?>> GetWithResultAsync(params object?[] keys) => GetWithResultAsync(keys, default);

        /// <summary>
        /// Gets the model for the specified <paramref name="keys"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c>.</returns>
        public Task<Result<TModel?>> GetWithResultAsync(object?[] keys, CancellationToken cancellationToken = default) => GetWithResultAsync(CompositeKey.Create(keys), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then <c>null</c> will be returned.</remarks>
        public Task<Result<TModel?>> GetWithResultAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetWithResultAsync(new EfDbArgs(EfDb.DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The model value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then <c>null</c> will be returned.</remarks>
        public Task<Result<TModel?>> GetWithResultAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => EfDb.GetWithResultAsync<TModel>(args, key, cancellationToken);

        #endregion
    }
}