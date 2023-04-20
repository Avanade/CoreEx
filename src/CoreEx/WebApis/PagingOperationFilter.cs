// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace CoreEx.WebApis
{
    /// <summary>
    /// A <i>Swagger/Swashbuckle</i> <see cref="IOperationFilter"/> to add the <see cref="PagingArgs"/> paramaters from the specification of the <see cref="PagingAttribute"/>.
    /// </summary>
    /// <remarks><para>The <see cref="PagingArgs"/> parameter names are sourced from <see cref="HttpConsts.PagingArgsSkipQueryStringName"/>, <see cref="HttpConsts.PagingArgsTakeQueryStringName"/> and <see cref="HttpConsts.PagingArgsCountQueryStringName"/>;
    /// and as such, can be overridden.</para>
    /// The <see cref="PagingOperationFilter"/> must be added when registering services (DI) during application startup; example as follows:
    /// <code>
    /// services.AddSwaggerGen(c =&gt; c.OperationFilter&lt;PagingOperationFilter&gt;());
    /// </code>
    /// </remarks>
    public class PagingOperationFilter : IOperationFilter
    { 
        /// <summary>
        /// Applies the filter.
        /// </summary>
        /// <param name="operation">The <see cref="OpenApiOperation"/>.</param>
        /// <param name="context">The <see cref="OperationFilterContext"/>.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var att = context.ApiDescription.CustomAttributes().OfType<PagingAttribute>().FirstOrDefault();
            if (att == null)
                return;

            operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsSkipQueryStringName, "The specified number of elements in a sequence to bypass.", "number"));
            operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsTakeQueryStringName, "The specified number of contiguous elements from the start of a sequence.", "number"));
            operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsCountQueryStringName, "Indicates whether to get the total count when performing the underlying query.", "boolean"));
        }

        /// <summary>
        /// Create the parameter definition.
        /// </summary>
        private static OpenApiParameter CreateParameter(string name, string description, string typeName) => new()
        {
            Name = name,
            Description = description,
            In = ParameterLocation.Query,
            Required = false,
            Schema = new OpenApiSchema { Type = typeName }
        };
    }
}