// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Hosting.Work;
using CoreEx.Results;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents the <see cref="WebApiPublisher.GetWorkResultAsync"/> arguments with no result value; being the opportunity to further configure the standard processing.
    /// </summary>
    /// <param name="typeName">The <see cref="WorkState.TypeName"/> enabling state separation.</param>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    public class WebApiPublisherResultArgs(string typeName, string? id)
    {
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

        /// <summary>
        /// Gets or sets the function to override the creation of the success <see cref="IActionResult"/>.
        /// </summary>
        /// <remarks>Defaults to a <see cref="ExtendedStatusCodeResult"/> with <see cref="HttpStatusCode.NoContent"/>.</remarks>
        public Func<WorkState, Task<IActionResult>>? CreateSuccessResultAsync { get; set; }
    }
}