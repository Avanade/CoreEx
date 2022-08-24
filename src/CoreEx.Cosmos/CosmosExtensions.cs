// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Azure.Cosmos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Provides <b>CosmosDb/DocumentDb</b>-related extension methods.
    /// </summary>
    public static class CosmosExtensions
    {
        /// <summary>
        /// Deletes the <see cref="Container"/> from the <see cref="Database"/>.
        /// </summary>
        /// <param name="database">The <see cref="Database"/>.</param>
        /// <param name="containerId">The container identifier.</param>
        /// <param name="requestOptions">The <see cref="ContainerRequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="ContainerResponse"/>.</returns>
        public static Task<ContainerResponse> DeleteContainerAsync(this Database database, string containerId, ContainerRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (containerId == null)
                throw new ArgumentNullException(nameof(containerId));

            var container = database.GetContainer(containerId);
            return container.DeleteContainerAsync(requestOptions, cancellationToken);
        }

        /// <summary>
        /// Replace or create the <see cref="Container"/>.
        /// </summary>
        /// <param name="database">The <see cref="Database"/>.</param>
        /// <param name="containerProperties">The <see cref="ContainerProperties"/>.</param>
        /// <param name="throughput">The throughput (RU/S).</param>
        /// <param name="requestOptions">The <see cref="RequestOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Container"/>.</returns>
        public static async Task<Container> ReplaceOrCreateContainerAsync(this Database database, ContainerProperties containerProperties, int? throughput = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (containerProperties == null)
                throw new ArgumentNullException(nameof(containerProperties));

            try
            {
                await database.DeleteContainerAsync(containerProperties.Id, null, cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException cex)
            {
                if (cex.StatusCode != System.Net.HttpStatusCode.NotFound)
                    throw;
            }

            return await database.CreateContainerAsync(containerProperties, throughput, requestOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}