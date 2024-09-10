// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// A <i>Swagger/Swashbuckle</i> <see cref="IOperationFilter"/> to add the <see cref="QueryArgs"/> parameters from the specification of the <see cref="QueryAttribute"/>.
    /// </summary>
    /// <remarks>The <see cref="PagingOperationFilter"/> must be added when registering services (DI) during application startup; example as follows:
    /// <code>
    /// services.AddSwaggerGen(c =&gt; c.OperationFilter&lt;PagingOperationFilter&gt;(PagingOperationFilterFields.SkipTakeCount));
    /// </code>
    /// </remarks>
    public class QueryOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOperationFilter"/> class with a default of <see cref="QueryOperationFilterFields.FilterAndOrderby"/>.
        /// </summary>
        public QueryOperationFilter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOperationFilter"/> class with the selected <paramref name="fields"/>.
        /// </summary>
        /// <param name="fields">The <see cref="QueryOperationFilterFields"/>.</param>
        public QueryOperationFilter(QueryOperationFilterFields fields) => Fields = fields;

        /// <summary>
        /// Gets the <see cref="QueryOperationFilterFields"/> to apply.
        /// </summary>
        public QueryOperationFilterFields Fields { get; } = QueryOperationFilterFields.FilterAndOrderby;

        /// <summary>
        /// Applies the filter.
        /// </summary>
        /// <param name="operation">The <see cref="OpenApiOperation"/>.</param>
        /// <param name="context">The <see cref="OperationFilterContext"/>.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var att = context.ApiDescription.CustomAttributes().OfType<QueryAttribute>().FirstOrDefault();
            if (att == null)
                return;

            if (Fields.HasFlag(QueryOperationFilterFields.Filter))
                operation.Parameters.Add(PagingOperationFilter.CreateParameter(HttpConsts.QueryArgsFilterQueryStringName, "The basic dynamic OData-like filter specification.", "string", null));

            if (Fields.HasFlag(QueryOperationFilterFields.OrderBy))
                operation.Parameters.Add(PagingOperationFilter.CreateParameter(HttpConsts.QueryArgsOrderByQueryStringName, "The basic dynamic OData-like order-by specificationswagger paramters .", "string", null));
        }
    }
}