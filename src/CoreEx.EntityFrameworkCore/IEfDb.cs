﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
        /// Gets the default <see cref="EfDbArgs"/> used where not expliticly specified for an operation.
        /// </summary>
        EfDbArgs DbArgs { get; }

        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="queryArgs">The <see cref="EfDbArgs"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        EfDbQuery<T, TModel> Query<T, TModel>(EfDbArgs queryArgs, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        Task<T?> GetAsync<T, TModel>(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        Task<T> CreateAsync<T, TModel>(EfDbArgs args, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes where specified).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        Task<T> UpdateAsync<T, TModel>(EfDbArgs args, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        Task DeleteAsync<T, TModel>(EfDbArgs args, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new();

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