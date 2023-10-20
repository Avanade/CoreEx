// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using CoreEx.Results;

namespace CoreEx.EntityFrameworkCore
{
    /// <summary>
    /// Provides the extended <b>Entity Framework</b> extension methods.
    /// </summary>
    public static class EfDbExtensions
    {
        /// <summary>
        /// Creates an <see cref="EfDbQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <param name="noTracking">Optionally override the specified/default <see cref="EfDbArgs.QueryNoTracking"/>.</param>
        /// <returns>A <see cref="EfDbQuery{T, TModel}"/>.</returns>
        public static EfDbQuery<T, TModel> Query<T, TModel>(this IEfDb efDb, Func<IQueryable<TModel>, IQueryable<TModel>>? query = null, bool? noTracking = null) where T : class, IEntityKey, new() where TModel : class, new()
        {
            var ea = new EfDbArgs(efDb.DbArgs);
            if (noTracking.HasValue)
                ea.QueryNoTracking = noTracking.Value;

            return efDb.Query<T, TModel>(ea, query);
        }

        #region Standard

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T, TModel>(this IEfDb efDb, EfDbArgs args, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() 
            => efDb.GetAsync<T, TModel>(args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T, TModel>(this IEfDb efDb, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() 
            => efDb.GetAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T, TModel>(this IEfDb efDb, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() 
            => efDb.GetAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), key, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<T> CreateAsync<T, TModel>(this IEfDb efDb, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() 
            => efDb.CreateAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<T> UpdateAsync<T, TModel>(this IEfDb efDb, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new() 
            => efDb.UpdateAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), value, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task DeleteAsync<T, TModel>(this IEfDb efDb, EfDbArgs args, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new() 
            => efDb.DeleteAsync<T, TModel>(args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task DeleteAsync<T, TModel>(this IEfDb efDb, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new() 
            => efDb.DeleteAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task DeleteAsync<T, TModel>(this IEfDb efDb, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new() 
            => efDb.DeleteAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), key, cancellationToken);

        #endregion

        #region WithResult

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T, TModel>(this IEfDb efDb, EfDbArgs args, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => efDb.GetWithResultAsync<T, TModel>(args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T, TModel>(this IEfDb efDb, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => efDb.GetWithResultAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T, TModel>(this IEfDb efDb, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => efDb.GetWithResultAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), key, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<Result<T>> CreateWithResultAsync<T, TModel>(this IEfDb efDb, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => efDb.CreateWithResultAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<Result<T>> UpdateWithResultAsync<T, TModel>(this IEfDb efDb, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => efDb.UpdateWithResultAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), value, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="args">The <see cref="EfDbArgs"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Result"/>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IEfDb efDb, EfDbArgs args, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => efDb.DeleteWithResultAsync<T, TModel>(args, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Result"/>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IEfDb efDb, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => efDb.DeleteWithResultAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Result"/>.</returns>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IEfDb efDb, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => efDb.DeleteWithResultAsync<T, TModel>(new EfDbArgs(efDb.DbArgs), key, cancellationToken);

        #endregion

        #region With

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, string? with, Action<string> action) => efDb.With(with, () => action(with!));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, int? with, Action<int> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, int with, Action<int> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, short? with, Action<short> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, short with, Action<short> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, long? with, Action<long> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, long with, Action<long> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, decimal? with, Action<decimal> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, decimal with, Action<decimal> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, float? with, Action<float> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, float with, Action<float> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, double? with, Action<double> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, double with, Action<double> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, DateTime? with, Action<DateTime> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, DateTime with, Action<DateTime> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, TimeSpan? with, Action<TimeSpan> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, TimeSpan with, Action<TimeSpan> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, bool? with, Action<bool> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, bool with, Action<bool> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, char? with, Action<char> action) => efDb.With(with, () => action(with!.Value));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>default</c>.
        /// </summary>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With(this IEfDb efDb, char with, Action<char> action) => efDb.With(with, () => action(with));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <param name="efDb"></param>
        /// <param name="with"></param>
        /// <param name="action"></param>
        public static void With<TRef>(this IEfDb efDb, TRef? with, Action<TRef> action) where TRef : IReferenceData => efDb.With(with, () => action(with!));

        /// <summary>
        /// Invokes the <paramref name="action"/> when the <paramref name="with"/> is not <c>null</c>.
        /// </summary>
        /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
        /// <param name="efDb">The <see cref="IEfDb"/>.</param>
        /// <param name="with">The value with which to verify.</param>
        /// <param name="action">The <see cref="Action"/> to invoke when there is a valid <paramref name="with"/> value.</param>
        public static void With<TRef>(this IEfDb efDb, ReferenceDataCodeList<TRef>? with, Action<ReferenceDataCodeList<TRef>> action) where TRef : class, IReferenceData, new() => efDb.With(with, () => action(with!));

        #endregion
    }
}