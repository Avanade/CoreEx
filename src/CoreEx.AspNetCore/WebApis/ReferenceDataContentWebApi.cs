// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using CoreEx.Json.Merge;
using CoreEx.RefData;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Provides the core (<see cref="HttpMethods.Get"/>, <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/> and <see cref="HttpMethods.Delete"/>) Web API execution encapsulation that uses the <see cref="IReferenceDataContentJsonSerializer"/>
    /// to allow <see cref="IReferenceData"/> types to serialize contents.
    /// </summary>
    public class ReferenceDataContentWebApi : WebApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApi"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="invoker">The <see cref="WebApiInvoker"/>; defaults where not specified.</param>
        /// <param name="jsonMergePatch">The <see cref="IJsonMergePatch"/> to support the <see cref="HttpMethods.Patch"/> operations.</param>
        public ReferenceDataContentWebApi(ExecutionContext executionContext, SettingsBase settings, IReferenceDataContentJsonSerializer jsonSerializer, ILogger<WebApi> logger, WebApiInvoker? invoker = null, IJsonMergePatch? jsonMergePatch = null)
            : base(executionContext, settings, jsonSerializer, logger, invoker, jsonMergePatch) { }
    }
}