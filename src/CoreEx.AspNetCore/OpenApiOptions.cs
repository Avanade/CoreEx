namespace CoreEx.AspNetCore;

/// <summary>
/// Provides the <i>OpenAPI</i> generated specification configuration settings.
/// </summary>
public class OpenApiOptions
{
    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; } = JsonDefaults.SerializerOptions;

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.PagingSkipQueryStringName"/> and <see cref="HttpNames.PagingSkipHeaderName"/>descriptive text.
    /// </summary>
    public string PagingSkipText { get; set; } = "The zero-based offset of the first item to return where paging.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.PagingTakeQueryStringName"/> and <see cref="HttpNames.PagingTakeHeaderName"/> descriptive text.
    /// </summary>
    public string PagingTakeText { get; set; } = "The maximum number of items to return where paging.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.PagingCountQueryStringName"/> descriptive text.
    /// </summary>
    public string PagingCountText { get; set; } = "Indicates whether to also attempt to include the total of matching items where paging.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.PagingTotalCountHeaderName"/> descriptive text.
    /// </summary>
    public string PagingTotalCountText { get; set; } = "The total number of matching items (only returned when `count=true`) where paging.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.QueryFilterQueryStringName"/> descriptive text.
    /// </summary>
    public string QueryFilterText { get; set; } = "The OData-like query filter expression.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.QueryOrderByQueryStringName"/> descriptive text.
    /// </summary>
    public string QueryOrderByText { get; set; } = "The OData-like query order-by expression.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.IncludeFieldsQueryStringName"/> descriptive text.
    /// </summary>
    public string IncludeFieldsText { get; set; } = "The comma separated list of JSON field names to include only in the response.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.ExcludeFieldsQueryStringName"/> descriptive text.
    /// </summary>
    public string ExcludeFieldsText { get; set; } = "The comma separated list of JSON field names to exclude from the response.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.IdempotencyKeyHeaderName"/> descriptive text.
    /// </summary>
    public string IdempotencyKeyText { get; set; } = "The idempotency key to use for the request (must be between 8 and 128 characters in length and consist of only letters, numbers, hyphens and underscores).";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.WarningMessagesHeaderName"/> descriptive text.
    /// </summary>
    public string WarningMessagesText { get; set; } = "Additional resulting warning message(s) where applicable.";

    /// <summary>
    /// Gets or sets the <see cref="HttpNames.InfoMessagesHeaderName"/> descriptive text.
    /// </summary>
    public string InfoMessagesText { get; set; } = "Additional resulting informational message(s) where applicable.";

    /// <summary>
    /// Indicates whether the <see cref="HttpNames.WarningMessagesHeaderName"/> and <see cref="HttpNames.InfoMessagesHeaderName"/> response headers should be included in the <i>OpenAPI</i> generated specification.
    /// </summary>
    public bool IncludeMessagesResponseHeaders { get; set; } = true;

    /// <summary>
    /// Indicates whether the 
    /// </summary>
    public bool IncludeFieldsRequestHeaders { get; set; } = true;

    /// <summary>
    /// Indicates whether the <see cref="HttpNames.PagingSkipHeaderName"/>, <see cref="HttpNames.PagingTakeHeaderName"/>, and <see cref="HttpNames.PagingTotalCountHeaderName"/> response headers should be included in the <i>OpenAPI</i> generated specification
    /// where the operation is a success (i.e. <see cref="HttpResponseMessage.StatusCode"/> is within the range 200-299).
    /// </summary>
    /// <remarks>The underlying operation must also have the <see cref="Mvc.PagingAttribute"/> metadata assigned.</remarks>
    public bool IncludePagingResponseHeaders { get; set; } = true;

    /// <summary>
    /// Indicates whether the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> should be included in the <i>OpenAPI</i> generated operation response specification for each <see cref="HttpStatusCode"/> specified within the <see cref="ProblemDetailsHttpStatusCodes"/>.
    /// </summary>
    public bool IncludeProblemDetailsHttpStatusCodes { get; set; } = true;

    /// <summary>
    /// Indicates whether the <see cref="Microsoft.AspNetCore.Mvc.ValidationProblemDetails"/> should be included in the <i>OpenAPI</i> generated operation response specification for each <see cref="HttpStatusCode"/> specified within the <see cref="ValidationProblemDetailsHttpStatusCodes"/>.
    /// </summary>
    public bool IncludeValidationProblemDetailsHttpStatusCodes { get; set; } = true;

    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/> list that are related to a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
    /// </summary>
    public List<HttpStatusCode> ProblemDetailsHttpStatusCodes { get; } =
    [
        HttpStatusCode.Unauthorized,
        HttpStatusCode.Forbidden,
        HttpStatusCode.Conflict,
        HttpStatusCode.PreconditionFailed,
        HttpStatusCode.ServiceUnavailable
    ];

    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/> list that are related to a <see cref="Microsoft.AspNetCore.Mvc.ValidationProblemDetails"/>.
    /// </summary>
    public List<HttpStatusCode> ValidationProblemDetailsHttpStatusCodes { get; } = [HttpStatusCode.BadRequest];
}