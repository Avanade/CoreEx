// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using Microsoft.Azure.Cosmos;
using System;
using System.Linq;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides the <b>CosmosDb/DocumentDb</b> capabilities.
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
        /// Gets the default <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.
        /// </summary>
        /// <remarks>Where <c>null</c> and the underlying <b>CosmosDb/DocumentDb</b> capability requires then <see cref="PartitionKey.None"/> will be used.</remarks>
        PartitionKey? PartitionKey { get; }

        /// <summary>
        /// Gets the specified <see cref="Container"/>.
        /// </summary>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The selected <see cref="Container"/>.</returns>
        Container GetCosmosContainer(string containerId);

        /// <summary>
        /// Gets (creates) the <see cref="CosmosDbContainer{T, TModel}"/> for the specified <paramref name="containerId"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The <see cref="CosmosDbContainer{T, TModel}"/>.</returns>
        CosmosDbContainer<T, TModel> Container<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IIdentifier<string>, new();

        /// <summary>
        /// Gets (creates) the <see cref="CosmosDbValueContainer{T, TModel}"/> for the specified <paramref name="containerId"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="containerId">The <see cref="Container"/> identifier.</param>
        /// <returns>The <see cref="CosmosDbValueContainer{T, TModel}"/>.</returns>
        CosmosDbValueContainer<T, TModel> ValueContainer<T, TModel>(string containerId) where T : class, IEntityKey, new() where TModel : class, IIdentifier, new();

        /// <summary>
        /// Invoked where a <see cref="CosmosException"/> has been thrown.
        /// </summary>
        /// <param name="cex">The <see cref="CosmosException"/>.</param>
        /// <remarks>Provides an opportunity to inspect and handle the exception before it bubbles up.</remarks>
        void HandleCosmosException(CosmosException cex);

        /// <summary>
        /// Gets or instantiates the <see cref="Microsoft.Azure.Cosmos.ItemRequestOptions"/>.
        /// </summary>
        /// <typeparam name="T">The entiy <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The <see cref="ItemRequestOptions"/>.</returns>
        ItemRequestOptions GetItemRequestOptions<T, TModel>(CosmosDbArgs dbArgs) where T : class, new() where TModel : class, IIdentifier, new();

        /// <summary>
        /// Gets or instantiates the <see cref="QueryRequestOptions"/>.
        /// </summary>
        /// <typeparam name="T">The entiy <see cref="Type"/>.</typeparam>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/>.</typeparam>
        /// <param name="dbArgs">The <see cref="CosmosDbArgs"/>.</param>
        /// <returns>The <see cref="QueryRequestOptions"/>.</returns>
        QueryRequestOptions GetQueryRequestOptions<T, TModel>(CosmosDbArgs dbArgs) where T : class, new() where TModel : class, IIdentifier, new();

        /// <summary>
        /// Gets the authorization filter.
        /// </summary>
        /// <typeparam name="TModel">The cosmos model <see cref="Type"/> persisted within the container.</typeparam>
        /// <param name="containerId">The <see cref="Microsoft.Azure.Cosmos.Container"/> identifier.</param>
        /// <returns>The filter query where found; otherwise, <c>null</c>.</returns>
        Func<IQueryable, IQueryable>? GetAuthorizeFilter<TModel>(string containerId);

        /// <summary>
        /// Formats an identifier to a <see cref="string"/> representation based on its underlying <see cref="Type"/> (used by the <see cref="ICosmosDbValue.PrepareBefore"/>).
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The identifier as a <see cref="string"/>.</returns>
        string FormatIdentifier(object? id);

        /// <summary>
        /// Parses a <see cref="string"/> identifier representation into its underlying <see cref="Type"/> (used by the <see cref="ICosmosDbValue.PrepareAfter"/>).
        /// </summary>
        /// <param name="type">The identifier <see cref="Type"/>.</param>
        /// <param name="id">The identifier as a <see cref="string"/>.</param>
        /// <returns>The parsed identifier.</returns>
        object? ParseIdentifier(Type type, string? id);
    }
}