// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.Azure.Cosmos;
using System;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides the <b>CosmosDb</b> capabilities.
    /// </summary>
    public interface ICosmosDb
    {
        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.Database"/>.
        /// </summary>
        Database Database { get; }

        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        IMapper Mapper { get; }

        /// <summary>
        /// Gets the <see cref="CosmosDbInvoker"/>.
        /// </summary>
        CosmosDbInvoker Invoker { get; }

        /// <summary>
        /// Gets the default <see cref="CosmosDbArgs"/> used where not expliticly specified for an operation.
        /// </summary>
        CosmosDbArgs DbArgs { get; }

        /// <summary>
        /// Gets the specified <see cref="Container"/>.
        /// </summary>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The selected <see cref="Container"/>.</returns>
        Container GetCosmosContainer(string containerId);

        /// <summary>
        /// Gets (or adds) the <see cref="CosmosDbContainer"/> for the specified <paramref name="containerId"/>.
        /// </summary>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The <see cref="CosmosDbContainer"/>.</returns>
        CosmosDbContainer Container(string containerId);

        /// <summary>
        /// Gets (or adds) the typed <see cref="CosmosDbContainer{T, TModel}"/> for the specified <typeparamref name="T"/> and <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The typed <see cref="CosmosDbContainer{T, TModel}"/></returns>
        CosmosDbContainer<T, TModel> Container<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new();

        /// <summary>
        /// Gets (or adds) the typed <see cref="CosmosDbValueContainer{T, TModel}"/> for the specified <typeparamref name="T"/> and <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The typed <see cref="CosmosDbValueContainer{T, TModel}"/></returns>
        CosmosDbValueContainer<T, TModel> ValueContainer<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IEntityKey, new();

        /// <summary>
        /// Invoked where a <see cref="CosmosException"/> has been thrown.
        /// </summary>
        /// <param name="cex">The <see cref="CosmosException"/>.</param>
        /// <returns>The <see cref="Result"/> containing the appropriate <see cref="IResult.Error"/> where handled; otherwise, <c>null</c> indicating that the exception is unexpected and will continue to be thrown as such.</returns>
        /// <remarks>Provides an opportunity to inspect and handle the exception before it is returned. A resulting <see cref="Result"/> that is <see cref="Result.IsSuccess"/> is not considered sensical; therefore, will result in the originating
        /// exception being thrown.</remarks>
        Result? HandleCosmosException(CosmosException cex);
    }
}