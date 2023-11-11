// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Enables the <b>OData</b> functionality.
    /// </summary>
    public interface IOData
    {
        /// <summary>
        /// Gets the underlying <see cref="Soc.ODataClient"/>.
        /// </summary>
        Soc.ODataClient Client { get; }

        /// <summary>
        /// Gets the <see cref="ODataInvoker"/>.
        /// </summary>
        ODataInvoker Invoker { get; }

        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        IMapper Mapper { get; }

        /// <summary>
        /// Gets the default <see cref="ODataClient.Args"/> used where not expliticly specified for an operation.
        /// </summary>
        ODataArgs Args { get; }

        /// <summary>
        /// Creates an <see cref="ODataQuery{T, TModel}"/> to enable select-like capabilities.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="queryArgs">The <see cref="ODataArgs"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="query">The function to further define the query.</param>
        /// <returns>A <see cref="ODataQuery{T, TModel}"/>.</returns>
        ODataQuery<T, TModel> Query<T, TModel>(ODataArgs queryArgs, string? collectionName, Func<Soc.IBoundClient<TModel>, Soc.IBoundClient<TModel>>? query = null) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Gets the entity for the specified <paramref name="key"/> mapping from <typeparamref name="TModel"/> to <typeparamref name="T"/> with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="Args"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        Task<Result<T?>> GetWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Creates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="Args"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        Task<Result<T>> CreateWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Updates the entity with a <see cref="Result{T}"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="Args"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="value">The value to insert.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The value (refreshed where specified).</returns>
        Task<Result<T>> UpdateWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, T value, CancellationToken cancellationToken = default) where T : class, IEntityKey, new() where TModel : class, new();

        /// <summary>
        /// Deletes the entity for the specified <paramref name="key"/> with a <see cref="Result"/>.
        /// </summary>
        /// <typeparam name="T">The resultant <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The entity framework model <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="Args"/>.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The entity value where found; otherwise, <c>null</c>.</returns>
        Task<Result> DeleteWithResultAsync<T, TModel>(ODataArgs args, string? collectionName, CompositeKey key, CancellationToken cancellationToken = default) where T : class, IEntityKey where TModel : class, new();

        /// <summary>
        /// Invoked where a <see cref="Soc.WebRequestException"/> has been thrown.
        /// </summary>
        /// <param name="odex">The <b>OData</b> <see cref="Soc.WebRequestException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/> where handled; otherwise, <c>null</c> indicating that the exception is unexpected and will continue to be thrown as such.</returns>
        /// <remarks>Provides an opportunity to inspect and handle the exception before it is returned. A resulting <see cref="Result"/> that is <see cref="Result.IsSuccess"/> is not considered sensical; therefore, will result in the originating
        /// exception being thrown.</remarks>
        Result? HandleODataException(Soc.WebRequestException odex);
    }
}