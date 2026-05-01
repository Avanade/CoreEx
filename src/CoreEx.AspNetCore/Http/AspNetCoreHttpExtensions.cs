using CoreEx.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace CoreEx.AspNetCore.Http;

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class AspNetCoreHttpExtensions
{
    /// <summary>
    /// Adds metadata to the <see cref="RouteHandlerBuilder"/> that the action/operation supports <see cref="PagingArgs"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="supportsCount">Indicates whether the <see cref="PagingArgs.IsCountRequested"/> is supported.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder WithPaging(this RouteHandlerBuilder builder, bool supportsCount = false) => builder.ThrowIfNull().WithMetadata(new PagingAttribute(supportsCount));

    /// <summary>
    /// Adds metadata to the <see cref="RouteHandlerBuilder"/> that the action/operation supports <see cref="QueryArgs"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="supportsFilter">Indicates whether <see cref="QueryArgs.Filter"/> is supported/enabled.</param>
    /// <param name="supportsOrderBy">Indicates whether <see cref="QueryArgs.OrderBy"/> is supported/enabled.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder WithQuery(this RouteHandlerBuilder builder, bool supportsFilter = true, bool supportsOrderBy = false) => builder.ThrowIfNull().WithMetadata(new QueryAttribute(supportsFilter, supportsOrderBy));

    /// <summary>
    /// Adds <see cref="Microsoft.AspNetCore.Http.Metadata.IAcceptsMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by the <paramref name="builder"/> defaulting the content type to <see cref="MediaTypeNames.Application.Json"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request <i>Body</i> <see cref="Type"/>.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder Accepts<TRequest>(this RouteHandlerBuilder builder) => builder.ThrowIfNull().Accepts(typeof(TRequest), MediaTypeNames.Application.Json);

    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> with a <see cref="ProblemDetails"/> type and <see cref="HttpStatusCode.NotFound"/> to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder ProducesNotFoundProblem(this RouteHandlerBuilder builder) => builder.ThrowIfNull().ProducesProblem((int)HttpStatusCode.NotFound);

    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> with a <see cref="HttpStatusCode.NoContent"/> to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder ProducesNoContent(this RouteHandlerBuilder builder) => builder.ThrowIfNull().Produces((int)HttpStatusCode.NoContent);

    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> with a <typeparamref name="TResponse"/> type and <see cref="HttpStatusCode.Created"/> to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder ProducesCreated<TResponse>(this RouteHandlerBuilder builder) => builder.ThrowIfNull().Produces<TResponse>((int)HttpStatusCode.Created);

    /// <summary>
    /// Adds metadata to the <see cref="RouteHandlerBuilder"/> that the action/operation supports the <see cref="HttpNames.IdempotencyKeyHeaderName"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="isRequired">Indicates whether an <see cref="HttpNames.IdempotencyKeyHeaderName"/> is required for the request.</param>
    /// <returns>The <see cref="RouteHandlerBuilder"/> to support fluent-style method-chaining.</returns>
    public static RouteHandlerBuilder WithIdempotencyKey(this RouteHandlerBuilder builder, bool isRequired = false) => builder.ThrowIfNull().WithMetadata(new IdempotencyKeyAttribute(isRequired));
}