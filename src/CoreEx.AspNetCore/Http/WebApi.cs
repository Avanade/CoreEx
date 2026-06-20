using AspHttp = Microsoft.AspNetCore.Http;

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

        IExtendedException? eex = null;

        if (result.Exception is not null)
        {
            if (!result.BypassExceptionLogging)
            {
                if (result.Exception is not IExtendedException reex || !reex.IsError)
                {
                    // Treat the unhandled exception as an error.
                    var logger = Logger ?? result.HttpResponse.HttpContext.RequestServices.GetRequiredService<ILogger<WebApi>>();
                    if (logger.IsEnabled(LogLevel.Error))
                        logger.LogError(result.Exception, "{Error}", result.Exception.Message);
                }
                else
                {
                    // Log the exception where required.
                    eex = reex;
                    var logger = Logger ?? result.HttpResponse.HttpContext.RequestServices.GetRequiredService<ILogger<WebApi>>();
                    if (eex.ShouldBeLogged && logger.IsEnabled(LogLevel.Error))
                        logger.LogError(result.Exception, "{Error}", eex.Message);
                }
            }
            else
                // Bypass logging, but still determine if the exception is an extended exception for later processing.
                eex = result.Exception as IExtendedException;

            // Merge all extensions.
            var extensions = CreateProblemDetailsExtensions(eex);
            if (eex is not null && eex.HasExtensions)
            {
                foreach (var kvp in eex.Extensions)
                {
                    extensions.TryAdd(kvp.Key, kvp.Value);
                }
            }

            // Special case where the exception is a ValidationException and has messages.
            if (eex is ValidationException vex && vex.Messages is not null && vex.Messages.Count > 0)
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

            // Convert and return exception as problem details.
            new WebApiHeader { RetryAfter = eex?.RetryAfter }.ApplyTo(this, result.HttpResponse);
            if (eex is not null)
                return AspHttp.Results.Problem(statusCode: (int)eex.StatusCode, title: eex.Message, detail: eex.Detail, extensions: extensions);

            var config = result.HttpResponse.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var include = Internal.GetConfigurationValue(IncludeExceptionInProblemDetailsName, false, config);

            if (include)
                return AspHttp.Results.Problem(statusCode: (int)HttpStatusCode.InternalServerError, title: result.Exception.Message, detail: result.Exception.ToString(), extensions: extensions);
            else
                return AspHttp.Results.Problem(statusCode: (int)HttpStatusCode.InternalServerError, title: new UnexpectedInternalException().Message, extensions: extensions);
        }

        result.Headers?.ApplyTo(this, result.HttpResponse);
        return result.Content is not null
            ? AspHttp.Results.Content(result.Content, result.ContentType ?? MediaTypeNames.Text.Plain, statusCode: (int)result.StatusCode)
            : AspHttp.Results.StatusCode((int)result.StatusCode);
    }
}