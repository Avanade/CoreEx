// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// A <i>Swagger/Swashbuckle</i> <see cref="IOperationFilter"/> to add the <see cref="PagingArgs"/> paramaters from the specification of the <see cref="PagingAttribute"/>.
    /// </summary>
    /// <remarks>The <see cref="PagingOperationFilter"/> must be added when registering services (DI) during application startup; example as follows:
    /// <code>
    /// services.AddSwaggerGen(c =&gt; c.OperationFilter&lt;PagingOperationFilter&gt;(PagingOperationFilterFields.SkipTakeCount));
    /// </code>
    /// </remarks>
    public class PagingOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PagingOperationFilter"/> class with a default of <see cref="PagingOperationFilterFields.SkipTakeCount"/>.
        /// </summary>
        public PagingOperationFilter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagingOperationFilter"/> class with the selected <paramref name="fields"/>.
        /// </summary>
        /// <param name="fields">The <see cref="PagingOperationFilterFields"/>.</param>
        public PagingOperationFilter(PagingOperationFilterFields fields) => Fields = fields;

        /// <summary>
        /// Gets the <see cref="PagingOperationFilterFields"/> to apply.
        /// </summary>
        public PagingOperationFilterFields Fields { get; } = PagingOperationFilterFields.SkipTakeCount;

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

            if (Fields.HasFlag(PagingOperationFilterFields.Skip))
                operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsSkipQueryStringName, "The specified number of elements in a sequence to bypass.", "number"));

            if (Fields.HasFlag(PagingOperationFilterFields.Take))
                operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsTakeQueryStringName, "The specified number of contiguous elements from the start of a sequence.", "number"));

            if (Fields.HasFlag(PagingOperationFilterFields.Page))
                operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsPageQueryStringName, "The page number for the elements in a sequence to select.", "number"));

            if (Fields.HasFlag(PagingOperationFilterFields.Size))
                operation.Parameters.Add(CreateParameter(HttpConsts.PagingArgsSizeQueryStringName, "The page size being the specified number of contiguous elements from the start of a sequence.", "number"));

            if (Fields.HasFlag(PagingOperationFilterFields.Count))
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