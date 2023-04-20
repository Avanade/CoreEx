// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace CoreEx.WebApis
{
    /// <summary>
    /// A <i>Swagger/Swashbuckle</i> <see cref="IOperationFilter"/> to infer the <see cref="OpenApiRequestBody"/> from the specification of the <see cref="AcceptsBodyAttribute"/>.
    /// </summary>
    /// <remarks>The <see cref="AcceptsBodyOperationFilter"/> must be added when registering services (DI) during application startup; example as follows:
    /// <code>
    /// services.AddSwaggerGen(c =&gt; c.OperationFilter&lt;AcceptsBodyOperationFilter&gt;());
    /// </code></remarks>
    public sealed class AcceptsBodyOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the filter.
        /// </summary>
        /// <param name="operation">The <see cref="OpenApiOperation"/>.</param>
        /// <param name="context">The <see cref="OperationFilterContext"/>.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Inspired by: https://stackoverflow.com/questions/66171439/swagger-parameter-on-method-with-parameters-from-body-but-no-model-binding
            var att = context.ApiDescription.CustomAttributes().OfType<AcceptsBodyAttribute>().FirstOrDefault();
            if (att == null)
                return;

            var schema = context.SchemaGenerator.GenerateSchema(att.BodyType, context.SchemaRepository);
            operation.RequestBody = new OpenApiRequestBody();
            att.ContentTypes.ForEach(item => operation.RequestBody.Content.Add(item, new OpenApiMediaType { Schema = schema }));
        }
    }
}