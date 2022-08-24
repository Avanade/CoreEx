// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Azure.Cosmos;
using System.Net;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides the <b>CosmosDb/DocumentDb Container</b> arguments.
    /// </summary>
    public struct CosmosDbArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct.
        /// </summary>
        public CosmosDbArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct for <b>Get</b>, <b>Create</b>, <b>Update</b>, and <b>Delete</b> operations with the specified <see cref="ItemRequestOptions"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="ItemRequestOptions"/>.</param>
        public CosmosDbArgs(ItemRequestOptions requestOptions) => ItemRequestOptions = requestOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbArgs"/> struct for <b>Query</b> operations with the specified <see cref="QueryRequestOptions"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="QueryRequestOptions"/>.</param>
        public CosmosDbArgs(QueryRequestOptions requestOptions) => QueryRequestOptions = requestOptions;

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.ItemRequestOptions"/> used for <b>Get</b>, <b>Create</b>, <b>Update</b>, and <b>Delete</b> (<seealso cref="QueryRequestOptions"/>).
        /// </summary>
        public ItemRequestOptions? ItemRequestOptions { get; set; } = null;

        /// <summary>
        /// Gets the <see cref="Microsoft.Azure.Cosmos.QueryRequestOptions"/> used for <b>Query</b> (<seealso cref="ItemRequestOptions"/>).
        /// </summary>
        public QueryRequestOptions? QueryRequestOptions { get; set; } = null;

        /// <summary>
        /// Indicates that a <c>null</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/> on <b>Get</b>.
        /// </summary>
        public bool NullOnNotFoundResponse { get; set; } = true;
    }
}