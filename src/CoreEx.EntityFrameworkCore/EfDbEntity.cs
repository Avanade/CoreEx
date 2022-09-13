// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides a lightweight typed <b>Entity Framework</b> wrapper over the <see cref="IEfDb"/> operations.
    /// </summary>
    /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
    /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
    public struct EfDbEntity<T, TModel> : IEfDbEntity where T : class, new() where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EfDbEntity{T, TModel}"/> class.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        public EfDbEntity(IEfDb efDb) => EfDb = efDb ?? throw new ArgumentNullException(nameof(efDb));

        /// <inheritdoc/>
        public IEfDb EfDb { get; }

        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        public EfDbQuery<T, TModel> Query(Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => Query(new EfDbArgs(EfDb.DbArgs), query);

        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <param name="queryArgs">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        public EfDbQuery<T, TModel> Query(EfDbArgs queryArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) => new(EfDb, queryArgs, query);

        /// <summary>
        /// Gets the entity for the specified <paramref name="keys"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<T?> GetAsync(params object?[] keys) => GetAsync(keys, default);

        /// <summary>
        /// Gets the entity for the specified <paramref name="keys"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<T?> GetAsync(object?[] keys, CancellationToken cancellationToken = default) => GetAsync(CompositeKey.Create(keys), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then <c>null</c> will be returned.</remarks>
        public Task<T?> GetAsync(CompositeKey key, CancellationToken cancellationToken = default) => GetAsync(new EfDbArgs(EfDb.DbArgs), key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then <c>null</c> will be returned.</remarks>
        public Task<T?> GetAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => EfDb.GetAsync<T, TModel>(args, key, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public Task<T> CreateAsync(T value, CancellationToken cancellationToken = default) => CreateAsync(new EfDbArgs(EfDb.DbArgs), value, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public Task<T> CreateAsync(EfDbArgs args, T value, CancellationToken cancellationToken = default) => EfDb.CreateAsync<T, TModel>(args, value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public Task<T> UpdateAsync(T value, CancellationToken cancellationToken = default) => UpdateAsync(new EfDbArgs(EfDb.DbArgs), value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public Task<T> UpdateAsync(EfDbArgs args, T value, CancellationToken cancellationToken = default) => EfDb.UpdateAsync<T, TModel>(args, value, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then a <see cref="NotFoundException"/> will be thrown.</remarks>
        public Task DeleteAsync(params object?[] keys) => DeleteAsync(keys, default);

        /// <summary>
        /// Performs a delete for the specified <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then a <see cref="NotFoundException"/> will be thrown.</remarks>
        public Task DeleteAsync(object?[] keys, CancellationToken cancellationToken = default) => DeleteAsync(CompositeKey.Create(keys), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then a <see cref="NotFoundException"/> will be thrown.</remarks>
        public Task DeleteAsync(CompositeKey key, CancellationToken cancellationToken = default) => DeleteAsync(new EfDbArgs(EfDb.DbArgs), key, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> and <see cref="ILogicallyDeleted.IsDeleted"/> is <c>true</c> then a <see cref="NotFoundException"/> will be thrown.</remarks>
        public Task DeleteAsync(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) => EfDb.DeleteAsync<T, TModel>(args, key, cancellationToken);
    }
}