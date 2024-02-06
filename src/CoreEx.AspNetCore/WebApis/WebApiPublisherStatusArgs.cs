// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Hosting.Work;
using CoreEx.Results;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents the <see cref="WebApiPublisher.GetWorkStatusAsync"/> arguments; being the opportunity to further configure the standard processing.
    /// </summary>
    /// <param name="typeName">The <see cref="WorkState.TypeName"/> enabling state separation.</param>
    /// <param name="id">The <see cref="WorkState.Id"/>.</param>
    public class WebApiPublisherStatusArgs(string typeName, string? id)
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
        /// Gets or sets the function to create the <see cref="Uri"/> for the result <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/>.
        /// </summary>
        /// <remarks>This will only be invoked when the <see cref="WorkState.Status"/> is <see cref="WorkStatus.Completed"/> and enables the <see cref="System.Net.HttpStatusCode.Redirect"/> <see cref="ExtendedStatusCodeResult"/> behavior.</remarks>
        public Func<WorkState, Uri>? CreateResultLocation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> for the <see cref="WorkStatus.Executing"/> <see cref="System.Net.Http.Headers.RetryConditionHeaderValue"/>.
        /// </summary>
        /// <remarks>This will only be invoked when the <see cref="WorkState.Status"/> is <see cref="WorkStatus.Executing"/>. Defaults to 30 seconds.</remarks>
        public TimeSpan? ExecutingRetryAfter { get; set; } = TimeSpan.FromSeconds(30);
    }
}