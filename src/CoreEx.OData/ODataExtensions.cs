// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.OData.Mapping;
using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Provides the <b>OData</b> extension methods.
    /// </summary>
    public static class ODataExtensions
    {
        /// <summary>
        /// Creates an <see cref="ODataQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="ODataQuery{T, TModel}"/>.</returns>
        public static ODataQuery<T, TModel> Query<T, TModel>(this IOData odata, Func<Soc.IBoundClient<TModel>, Soc.IBoundClient<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.Query<T, TModel>(new ODataArgs(odata.Args), null, query);

        /// <summary>
        /// Creates an <see cref="ODataQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="ODataQuery{T, TModel}"/>.</returns>
        public static ODataQuery<T, TModel> Query<T, TModel>(this IOData odata, string? collectionName, Func<Soc.IBoundClient<TModel>, Soc.IBoundClient<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.Query<T, TModel>(new ODataArgs(odata.Args), collectionName, query);

        #region Standard

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T, TModel>(this IOData odata, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.GetAsync<T, TModel>(CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T, TModel>(this IOData odata, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.GetAsync<T, TModel>(null, key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<T?> GetAsync<T, TModel>(this IOData odata, string? collectionName, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.GetAsync<T, TModel>(collectionName, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static async Task<T?> GetAsync<T, TModel>(this IOData odata, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => (await odata.GetWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<T> CreateAsync<T, TModel>(this IOData odata, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.CreateAsync<T, TModel>(null, value, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static async Task<T> CreateAsync<T, TModel>(this IOData odata, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => (await odata.CreateWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<T> UpdateAsync<T, TModel>(this IOData odata, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.UpdateAsync<T, TModel>(null, value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes).
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static async Task<T> UpdateAsync<T, TModel>(this IOData odata, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => (await odata.UpdateWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, value, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task DeleteAsync<T, TModel>(this IOData odata, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteAsync<T, TModel>(CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task DeleteAsync<T, TModel>(this IOData odata, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteAsync<T, TModel>(null, key, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task DeleteAsync<T, TModel>(this IOData odata, string? collectionName, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteAsync<T, TModel>(collectionName, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static async Task DeleteAsync<T, TModel>(this IOData odata, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => (await odata.DeleteWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, key, cancellationToken).ConfigureAwait(false)).ThrowOnError();

        #endregion

        #region WithResult

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T, TModel>(this IOData odata, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.GetWithResultAsync<T, TModel>(CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T, TModel>(this IOData odata, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.GetWithResultAsync<T, TModel>(null, key, cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static Task<Result<T?>> GetWithResultAsync<T, TModel>(this IOData odata, string? collectionName, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.GetWithResultAsync<T, TModel>(collectionName, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        public static async Task<Result<T?>> GetWithResultAsync<T, TModel>(this IOData odata, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => (await odata.GetWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, key, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<Result<T>> CreateWithResultAsync<T, TModel>(this IOData odata, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.CreateWithResultAsync<T, TModel>(null, value, cancellationToken);

        /// <summary>
        /// Performs a create for the value (reselects and/or automatically saves changes) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<Result<T>> CreateWithResultAsync<T, TModel>(this IOData odata, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.CreateWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<Result<T>> UpdateWithResultAsync<T, TModel>(this IOData odata, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.UpdateWithResultAsync<T, TModel>(null, value, cancellationToken);

        /// <summary>
        /// Performs an update for the value (reselects and/or automatically saves changes) with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        public static Task<Result<T>> UpdateWithResultAsync<T, TModel>(this IOData odata, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new()
            => odata.UpdateWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, value, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IOData odata, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteWithResultAsync<T, TModel>(CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IOData odata, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteWithResultAsync<T, TModel>(null, key, cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The key value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IOData odata, string? collectionName, object? key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteWithResultAsync<T, TModel>(collectionName, CompositeKey.Create(key), cancellationToken);

        /// <summary>
        /// Performs a delete for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Where the model implements <see cref="ILogicallyDeleted"/> then this will update the <see cref="ILogicallyDeleted.IsDeleted"/> with <c>true</c> versus perform a physical deletion.</remarks>
        public static Task<Result> DeleteWithResultAsync<T, TModel>(this IOData odata, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new()
            => odata.DeleteWithResultAsync<T, TModel>(new ODataArgs(odata.Args), collectionName, key, cancellationToken);

        #endregion

        #region CreateItemCollection

        /// <summary>
        /// Creates an untyped <see cref="ODataItemCollection{T}"/> for the specified <paramref name="collectionName"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TMapper">The <see cref="IODataMapper{TSource}"/> <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The <see cref="ODataItemCollection{T}"/>.</returns>
        public static ODataItemCollection<T> CreateItemCollection<T, TMapper>(this IOData odata, string collectionName) where T : class, new() where TMapper : IODataMapper<T>, new()
            => odata.CreateItemCollection(collectionName, new TMapper());

        /// <summary>
        /// Creates an untyped <see cref="ODataItemCollection{T}"/> for the specified <paramref name="collectionName"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TMapper">The <see cref="IODataMapper{TSource}"/> <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="ODataArgs"/>.</param>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <returns>The <see cref="ODataItemCollection{T}"/>.</returns>
        public static ODataItemCollection<T> CreateItemCollection<T, TMapper>(this IOData odata, ODataArgs args, string collectionName) where T : class, new() where TMapper : IODataMapper<T>, new()
            => odata.CreateItemCollection(args, collectionName, new TMapper());

        /// <summary>
        /// Creates an untyped <see cref="ODataItemCollection{T}"/> for the specified <paramref name="collectionName"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <param name="odata">The <see cref="IOData"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="mapper">The specific <see cref="IODataMapper{TSource}"/>.</param>
        /// <returns>The <see cref="ODataItemCollection{T}"/>.</returns>
        public static ODataItemCollection<T> CreateItemCollection<T>(this IOData odata, string collectionName, IODataMapper<T> mapper) where T : class, new()
            => odata.CreateItemCollection(new ODataArgs(odata.Args), collectionName, mapper);

        #endregion
    }
}