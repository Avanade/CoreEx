﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using Microsoft.Azure.Cosmos;
using System;
using System.Net;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides the <b>CosmosDb Container</b> arguments.
    /// </summary>
    public struct CosmosDbArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct.
        /// </summary>
        public CosmosDbArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct.
        /// </summary>
        /// <param name="template">The template <see cref="CosmosDbArgs"/> to copy from.</param>
        /// <param name="partitionKey">The override <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
        public CosmosDbArgs(CosmosDbArgs template, PartitionKey? partitionKey = null)
        {
            PartitionKey = partitionKey ?? template.PartitionKey;
            ItemRequestOptions = template.ItemRequestOptions;
            QueryRequestOptions = template.QueryRequestOptions;
            NullOnNotFound = template.NullOnNotFound;
            AutoMapETag = template.AutoMapETag;
            CleanUpResult = template.CleanUpResult;
            FilterByTenantId = template.FilterByTenantId;
            GetTenantId = template.GetTenantId;
            FormatIdentifier = template.FormatIdentifier;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct.
        /// </summary>
        /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
        public CosmosDbArgs(PartitionKey partitionKey) => PartitionKey = partitionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct for <b>Get</b>, <b>Create</b>, <b>Update</b>, and <b>Delete</b> operations with the specified <see cref="ItemRequestOptions"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
        public CosmosDbArgs(ItemRequestOptions requestOptions, PartitionKey? partitionKey = null)
        {
            ItemRequestOptions = requestOptions;
            PartitionKey = partitionKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct for <b>Query</b> operations with the specified <see cref="QueryRequestOptions"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="QueryRequestOptions"/>.</param>
        /// <param name="partitionKey">The <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.</param>
        public CosmosDbArgs(QueryRequestOptions requestOptions, PartitionKey? partitionKey = null)
        {
            QueryRequestOptions = requestOptions;
            PartitionKey = partitionKey;
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.PartitionKey"/>.
        /// </summary>
        public PartitionKey? PartitionKey { get; } = null;

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.ItemRequestOptions"/> used for <b>Get</b>, <b>Create</b>, <b>Update</b>, and <b>Delete</b> (<seealso cref="QueryRequestOptions"/>).
        /// </summary>
        public ItemRequestOptions? ItemRequestOptions { get; } = null;

        /// <summary>
        /// Gets (or creates new) the <see cref="ItemRequestOptions"/>.
        /// </summary>
        public readonly ItemRequestOptions GetItemRequestOptions() => ItemRequestOptions ?? new ItemRequestOptions();

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.QueryRequestOptions"/> used for <b>Query</b> (<seealso cref="ItemRequestOptions"/>).
        /// </summary>
        public QueryRequestOptions? QueryRequestOptions { get; } = null;

        /// <summary>
        /// Gets (or creates new) the <see cref="QueryRequestOptions"/>.
        /// </summary>
        public readonly QueryRequestOptions GetQueryRequestOptions() => UpdateQueryRequestionOptionsPartitionKey(QueryRequestOptions ?? new QueryRequestOptions());

        /// <summary>
        /// Updates the <see cref="QueryRequestOptions"/> with the <see cref="PartitionKey"/> where not already set.
        /// </summary>
        private readonly QueryRequestOptions UpdateQueryRequestionOptionsPartitionKey(QueryRequestOptions qro)
        {
            qro.PartitionKey ??= PartitionKey;
            return qro;
        }

        /// <summary>
        /// Indicates whether a <c>null</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/> on <b>Get</b>. Defaults to <c>true</c>.
        /// </summary>
        public bool NullOnNotFound { get; set; } = true;

        /// <summary>
        /// Indicates whether when mapping the model to the corresponding entity that the <see cref="IETag.ETag"/> is to be automatically mapped. Defaults to <c>true</c>.
        /// </summary>
        public bool AutoMapETag { get; set; } = true;

        /// <summary>
        /// Indicates whether the result should be <see cref="Entities.Cleaner.Clean{T}(T)">cleaned up</see>. Defaults to <c>false</c>.
        /// </summary>
        public bool CleanUpResult { get; set; } = false;

        /// <summary>
        /// Indicates that when the underlying model implements <see cref="Entities.ITenantId.TenantId"/> it is to be filtered by the corresponding <see cref="GetTenantId"/> value. Defaults to <c>true</c>.
        /// </summary>
        public bool FilterByTenantId { get; set; }

        /// <summary>
        /// Gets or sets the <i>get</i> tenant identifier function; defaults to <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.TenantId"/>.
        /// </summary>
        public Func<string?> GetTenantId { get; set; } = () => ExecutionContext.HasCurrent ? ExecutionContext.Current.TenantId : null;

        /// <summary>
        /// Formats a <see cref="CompositeKey"/> to a <see cref="string"/> representation (used by <see cref="CosmosDbContainerBase{T, TModel, TSelf}.GetCosmosId(T)"/> and <see cref="ICosmosDbValue.PrepareBefore"/>).
        /// </summary>
        /// <returns>The identifier as a <see cref="string"/>.</returns>
        /// <remarks>Defaults to <see cref="DefaultFormatIdentifier"/>.</remarks>
        public Func<CompositeKey, string?> FormatIdentifier { get; set; } = DefaultFormatIdentifier;

        /// <summary>
        /// Provides the default <see cref="FormatIdentifier"/> implementation; being the <see cref="CompositeKey"/> <see cref="object.ToString"/>.
        /// </summary>
        public static Func<CompositeKey, string?> DefaultFormatIdentifier { get; } = key => key.ToString();
    }
}