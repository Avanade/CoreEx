namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// Provides the foundation (<see cref="HttpMethods.Get"/>, <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/> and <see cref="HttpMethods.Delete"/>) ASP.NET Core MVC Web API execution encapsulation.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonDefaults"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/> for the <see cref="WebApi"/>.</param>
/// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
/// <remarks>The <see cref="HttpMethods.Get"/> methods within can also be used for a <see cref="HttpMethods.Head"/> as it is essentially the same operation without a corresponding response; this distinction is handled internally.
/// <para>Unexpected exceptions propagate unless <see cref="WebApiBase.ConvertUnhandledExceptionsToProblemDetails"/> is enabled.</para></remarks>
public class WebApi(JsonSerializerOptions? jsonSerializerOptions = null, ILogger<WebApi>? logger = null, ExecutionContext? executionContext = null) : WebApi<IActionResult>(WebApiInvoker.Default, jsonSerializerOptions, logger, executionContext)
{
    /// <inheritdoc/>
    internal override IActionResult CreateResult(WebApiResult<IActionResult> result)
    {
        if (result.Result is not null)
            return result.Result;

        if (result.Exception is not null)
        {
            var extendedException = PrepareException<WebApi>(result.HttpResponse, result.Exception, result.BypassExceptionLogging);
            return CreateResultForException(result.HttpResponse.HttpContext, result.Exception, extendedException);
        }

        result.Headers?.ApplyTo(this, result.HttpResponse);
        return result.Content is not null
            ? new ContentResult { Content = result.Content, ContentType = result.ContentType ?? MediaTypeNames.Text.Plain, StatusCode = (int)result.StatusCode }
            : new StatusCodeResult((int)result.StatusCode);
    }

    /// <summary>
    /// Creates the response representation for an exception already accepted, classified, and logged by WebApi.
    /// </summary>
    /// <param name="httpContext">The originating request and response context.</param>
    /// <param name="exception">The original exception. Internal diagnostics must not be exposed in a specialized response.</param>
    /// <param name="extendedException">The classified exception, or <see langword="null"/> for an unexpected failure.</param>
    /// <returns>The exception response, defaulting to <see cref="ProblemDetails"/>.</returns>
    /// <remarks>Overriding changes representation only. Propagation policy, logging, and applicable Retry-After headers are handled before this method. Do not log the failure again.
    /// Success media types do not apply. This override does not enable <see cref="WebApiBase.ConvertUnhandledExceptionsToProblemDetails"/>.</remarks>
    protected virtual IActionResult CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)
    {
        var pdf = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        if (extendedException is ValidationException vex && vex.Messages is not null && vex.Messages.Count > 0)
        {
            var msd = new ModelStateDictionary();
            foreach (var item in vex.Messages.GetMessagesForType(MessageType.Error))
            {
                if (item.Property is not null && item.Text is not null)
                    msd.AddModelError(item.Property, item.Text.ToString()!);
            }

            var vpd = pdf.CreateValidationProblemDetails(httpContext, msd, title: vex.Message, detail: vex.Detail);
            AddExtensions(vpd.Extensions, CreateProblemDetailsExtensions(extendedException));
            if (extendedException.HasExtensions)
                AddExtensions(vpd.Extensions, extendedException.Extensions);

            return new BadRequestObjectResult(vpd);
        }

        ProblemDetails pd;
        if (extendedException is not null)
        {
            pd = pdf.CreateProblemDetails(httpContext, (int)extendedException.StatusCode, title: extendedException.Message, detail: extendedException.Detail);
            AddExtensions(pd.Extensions, CreateProblemDetailsExtensions(extendedException));
            if (extendedException.HasExtensions)
                AddExtensions(pd.Extensions, extendedException.Extensions);

            return new ObjectResult(pd) { StatusCode = (int)extendedException.StatusCode };
        }

        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var include = Internal.GetConfigurationValue(IncludeExceptionInProblemDetailsName, false, config);
        pd = include
            ? pdf.CreateProblemDetails(httpContext, (int)HttpStatusCode.InternalServerError, title: exception.Message, detail: exception.ToString())
            : pdf.CreateProblemDetails(httpContext, (int)HttpStatusCode.InternalServerError, title: new UnexpectedInternalException().Message, detail: null);

        AddExtensions(pd.Extensions, CreateProblemDetailsExtensions(null));
        return new ObjectResult(pd) { StatusCode = (int)HttpStatusCode.InternalServerError };
    }

    /// <summary>
    /// Adds extension values without replacing existing entries.
    /// </summary>
    private static void AddExtensions(IDictionary<string, object?> extensions, IDictionary<string, object?> extended)
    {
        foreach (var kvp in extended)
        {
            extensions.TryAdd(kvp.Key, kvp.Value);
        }
    }
}
