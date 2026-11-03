namespace CoreEx.AspNetCore.Http;

/// <summary>
/// Provides the foundation (<see cref="HttpMethods.Get"/>, <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/> and <see cref="HttpMethods.Delete"/>) ASP.NET Core Minimal Web API execution encapsulation.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonDefaults"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/> for the <see cref="WebApi"/>.</param>
/// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
/// <remarks>The <see cref="HttpMethods.Get"/> methods within can also be used for a <see cref="HttpMethods.Head"/> as it is essentially the same operation without a corresponding response; this distinction is handled internally.</remarks>
public class WebApi(JsonSerializerOptions? jsonSerializerOptions = null, ILogger<WebApi>? logger = null, ExecutionContext? executionContext = null) : WebApi<IResult>(WebApiInvoker.Default, jsonSerializerOptions, logger, executionContext)
{
    /// <inheritdoc/>
    internal override IResult CreateResult(WebApiResult<IResult> result)
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
            ? AspHttp.Results.Content(result.Content, result.ContentType ?? MediaTypeNames.Text.Plain, statusCode: (int)result.StatusCode)
            : AspHttp.Results.StatusCode((int)result.StatusCode);
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
    protected virtual IResult CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)
    {
        var extensions = CreateProblemDetailsExtensions(extendedException);
        if (extendedException is not null && extendedException.HasExtensions)
        {
            foreach (var kvp in extendedException.Extensions)
            {
                extensions.TryAdd(kvp.Key, kvp.Value);
            }
        }

        if (extendedException is ValidationException vex && vex.Messages is not null && vex.Messages.Count > 0)
        {
            var msd = new Dictionary<string, string[]>();
            foreach (var item in from m in vex.Messages.GetMessagesForType(MessageType.Error).Where(x => x.Property is not null && x.Text is not null)
                                 group m by m.Property into g
                                 select new { Property = g.Key, Messages = g })
            {
                msd.Add(item.Property!, [.. item.Messages.Select(m => m.Text!.ToString()!)]);
            }

            return AspHttp.Results.ValidationProblem(msd, title: vex.Message, detail: vex.Detail, extensions: extensions);
        }

        if (extendedException is not null)
            return AspHttp.Results.Problem(statusCode: (int)extendedException.StatusCode, title: extendedException.Message, detail: extendedException.Detail, extensions: extensions);

        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var include = Internal.GetConfigurationValue(IncludeExceptionInProblemDetailsName, false, config);
        return include
            ? AspHttp.Results.Problem(statusCode: (int)HttpStatusCode.InternalServerError, title: exception.Message, detail: exception.ToString(), extensions: extensions)
            : AspHttp.Results.Problem(statusCode: (int)HttpStatusCode.InternalServerError, title: new UnexpectedInternalException().Message, extensions: extensions);
    }
}
