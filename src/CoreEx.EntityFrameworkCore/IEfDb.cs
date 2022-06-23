// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database;
using CoreEx.Entities;
using CoreEx.Mapping;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Enables the extended <b>Entity Framework</b> functionality.
    /// </summary>
    public interface IEfDb
    {
        /// <summary>
        /// Gets the underlying <see name="Microsoft.EntityFrameworkCore.DbContext"/>.
        /// </summary>
        DbContext DbContext { get; }

        /// <summary>
        /// Gets the <see cref="EfDbInvoker"/>.
        /// </summary>
        EfDbInvoker Invoker { get; }

        /// <summary>
        /// Gets the <see cref="IDatabase"/>.
        /// </summary>
        IDatabase Database { get; }

        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        IMapper Mapper { get; }

        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="queryArgs">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        EfDbQuery<T, TModel> Query<T, TModel>(EfDbArgs queryArgs, Func<IQueryable<TModel>, EfDbArgs, IQueryable<TModel>>? query = null) where T : class, new() where TModel : class, new();

        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        public EfDbQuery<T, TModel> Query<T, TModel>(Func<IQueryable<TModel>, EfDbArgs, IQueryable<TModel>>? query = null) where T : class, new() where TModel : class, new() => Query<T, TModel>(EfDbArgs.Create(), query);

        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="pagingArgs">The <see cref="PagingArgs"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        public EfDbQuery<T, TModel> Query<T, TModel>(PagingArgs pagingArgs, Func<IQueryable<TModel>, EfDbArgs, IQueryable<TModel>>? query = null) where T : class, new() where TModel : class, new() => Query<T, TModel>(EfDbArgs.Create(pagingArgs), query);

        /// <summary>
        /// Performs an <see cref="Query{T, TModel}(PagingArgs, Func{IQueryable{TModel}, EfDbArgs, IQueryable{TModel}}?)"/> to create and update a resulting <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="paging">The <see cref="PagingResult"/> or <see cref="PagingArgs"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/> instance.</returns>
        Task<TCollResult> SelectResultQueryAsync<TCollResult, TColl, T, TModel>(PagingArgs? paging = null, Func<IQueryable<TModel>, EfDbArgs, IQueryable<TModel>>? query = null, CancellationToken cancellationToken = default)
            where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() where T : class, new() where TModel : class, new();

        /// <summary>
        /// Performs an <see cref="Query{T, TModel}(PagingArgs, Func{IQueryable{TModel}, EfDbArgs, IQueryable{TModel}}?)"/> to create and update a resulting <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="query">The function to further define the query.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/> instance.</returns>
        public Task<TCollResult> SelectResultQueryAsync<TCollResult, TColl, T, TModel>(Func<IQueryable<TModel>, EfDbArgs, IQueryable<TModel>>? query = null, CancellationToken cancellationToken = default)
            where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() where T : class, new() where TModel : class, new()
            => SelectResultQueryAsync<TCollResult, TColl, T, TModel>(null, query, cancellationToken);

        /// <summary>
        /// Performs an <see cref="Query{T, TModel}(PagingArgs, Func{IQueryable{TModel}, EfDbArgs, IQueryable{TModel}}?)"/> to create and update a resulting <typeparamref name="TCollResult"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The <see cref="ICollection{T}"/> <see cref="Type"/>.</typeparam>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="paging">The <see cref="PagingResult"/> or <see cref="PagingArgs"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The resulting <typeparamref name="TCollResult"/> instance.</returns>
        Task<TCollResult> SelectResultQueryAsync<TCollResult, TColl, T, TModel>(PagingArgs paging, CancellationToken cancellationToken = default)
            where TCollResult : ICollectionResult<TColl, T>, new() where TColl : ICollection<T>, new() where T : class, new() where TModel : class, new()
            => SelectResultQueryAsync<TCollResult, TColl, T, TModel>(paging, null, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="keys"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="keys">The key values.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<T?> GetAsync<T, TModel>(EfDbArgs args, params object?[] keys) where T : class, new() where TModel : class, new() => GetAsync<T, TModel>(args, CompositeKey.Create(keys), CancellationToken.None);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        Task<T?> GetAsync<T, TModel>(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken) where T : class, new() where TModel : class, new();

        /// <summary>
        /// Gets the entity for the specified <paramref name="keys"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="keys">The key values.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<T?> GetAsync<T, TModel>(params object?[] keys) where T : class, new() where TModel : class, new() => GetAsync<T, TModel>(EfDbArgs.Create(), CompositeKey.Create(keys));

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public Task<T?> GetAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken) where T : class, new() where TModel : class, new() => GetAsync<T, TModel>(EfDbArgs.Create(), key, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        Task<T> CreateAsync<T, TModel>(EfDbArgs args, T value, CancellationToken cancellationToken) where T : class, new() where TModel : class, new();

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public Task<T> CreateAsync<T, TModel>(T value, CancellationToken cancellationToken) where T : class, new() where TModel : class, new() => CreateAsync<T, TModel>(EfDbArgs.Create(), value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        Task<T> UpdateAsync<T, TModel>(EfDbArgs args, T value, CancellationToken cancellationToken) where T : class, new() where TModel : class, new();

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public Task<T> UpdateAsync<T, TModel>(T value, CancellationToken cancellationToken) where T : class, new() where TModel : class, new() => UpdateAsync<T, TModel>(EfDbArgs.Create(), value, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="keys"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="keys">The key values.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public Task DeleteAsync<T, TModel>(EfDbArgs args, params object?[] keys) where T : class where TModel : class, new() => DeleteAsync<T, TModel>(args, CompositeKey.Create(keys), CancellationToken.None);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        Task DeleteAsync<T, TModel>(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken) where T : class where TModel : class, new();

        /// <summary>
        /// Performs a delete for the specified <paramref name="keys"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="keys">The key values.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public Task DeleteAsync<T, TModel>(params object?[] keys) where T : class where TModel : class, new() => DeleteAsync<T, TModel>(EfDbArgs.Create(), CompositeKey.Create(keys));

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public Task DeleteAsync<T, TModel>(CompositeKey key, CancellationToken cancellationToken) where T : class where TModel : class, new() => DeleteAsync<T, TModel>(EfDbArgs.Create(), key, cancellationToken);

        /// <summary>
        /// Invokes the <paramref name="action"/> whilst <see cref="DatabaseWildcard.Replace(string)">replacing</see> the <b>wildcard</b> characters when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value; passed the database specific wildcard value as the action argument.</param>
        void WithWildcard(string? with, Action<string> action);

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not the default value for the <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The with value <see cref="Type"/>.</typeparam>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        void With<T>(T with, Action action);
    }
}