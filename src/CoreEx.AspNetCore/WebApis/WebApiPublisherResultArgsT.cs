// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Hosting.Work;
using CoreEx.Results;
using System;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents the <see cref="WebApiPublisher.GetWorkResultAsync"/> arguments with a result value; being the opportunity to further configure the standard processing.
    /// </summary>
    /// <typeparam name="TValue">The resulting value <see cref="Type"/>.</typeparam>
    /// <param name="typeName">The <see cref="WorkState.TypeName"/> enabling state separation.</param>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    public class WebApiPublisherResultArgs<TValue>(string typeName, string? id)
    {
        /// <summary>
        /// Gets the resulting <typeparamref name="TValue"/> <see cref="Type"/>.
        /// </summary>
        public Type ValueType => typeof(TValue);

        /// <summary>
        /// Gets the <see cref="WorkStateOrchestrator"/> type name.
        /// </summary>
        /// <remarks>Enables separation between one or more <see cref="WorkState"/> types.</remarks>
        public string TypeName => typeName.ThrowIfNullOrEmpty(nameof(typeName));

        /// <summary>
        /// Gets or sets the <see cref="WorkState.Id"/>.
        /// </summary>
        public string? Id { get; set; } = id;

        /// <summary>
        /// Gets or sets the function to execute prior to returning the response.
        /// </summary>
        /// <remarks>Enables the likes of security, etc., before returning the response. The <see cref="Result"/> will allow failures and alike to be returned where applicable.</remarks>
        public Func<WorkState, Task<Result>>? OnBeforeResponseAsync { get; set; }
    }
}