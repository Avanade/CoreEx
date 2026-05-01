using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CoreEx.AspNetCore.Mvc;

/// <summary>
/// Provides the foundation (<see cref="HttpMethods.Get"/>, <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/> and <see cref="HttpMethods.Delete"/>) ASP.NET Core MVC Web API execution encapsulation.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonDefaults"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/> for the <see cref="WebApi"/>.</param>
/// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
/// <remarks>The <see cref="HttpMethods.Get"/> methods within can also be used for a <see cref="HttpMethods.Head"/> as it is essentially the same operation without a corresponding response; this distinction is handled internally.
/// <para>Any <see cref="WebApiResult{TResult}.Exception"/> that is not an <see cref="IExtendedException"/> and not <see cref="IExtendedException.IsError"/> will be thrown.</para></remarks>
public class WebApi(JsonSerializerOptions? jsonSerializerOptions = null, ILogger<WebApi>? logger = null, ExecutionContext? executionContext = null) : WebApi<IActionResult>(WebApiInvoker.Default, jsonSerializerOptions, logger, executionContext)
{
    /// <inheritdoc/>
    internal override IActionResult CreateResult(WebApiResult<IActionResult> result)
    {
        static void AddExtensions(IDictionary<string, object?> extensions, IDictionary<string, object?> extended)
        {
            foreach (var kvp in extended)
            {
                extensions.TryAdd(kvp.Key, kvp.Value);
            }
        }

        if (result.Result is not null)
            return result.Result;

        IExtendedException? eex = null;

        if (result.Exception is not null)
        {
            if (!result.BypassExceptionLogging)
            {
                if (result.Exception is not IExtendedException reex || !reex.IsError)
                {
                    if (!ConvertUnhandledExceptionsToProblemDetails)
                        throw result.Exception;

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

            var pdf = result.HttpResponse.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

            // Special case where the exception is a ValidationException and has messages.
            if (eex is ValidationException vex && vex.Messages is not null && vex.Messages.Count > 0)
            {
                var msd = new ModelStateDictionary();
                foreach (var item in vex.Messages.GetMessagesForType(MessageType.Error))
                {
                    if (item.Property is not null && item.Text is not null)
                        msd.AddModelError(item.Property, item.Text.ToString()!);
                }

                var vpd = pdf.CreateValidationProblemDetails(result.HttpResponse.HttpContext, msd, title: vex.Message, detail: vex.Detail);
                AddExtensions(vpd.Extensions, CreateProblemDetailsExtensions(eex));
                if (eex.HasExtensions)
                    AddExtensions(vpd.Extensions, eex.Extensions);

                return new BadRequestObjectResult(vpd);
            }

            // Apply the Retry-After header where applicable.
            if (eex is not null && eex.RetryAfter.HasValue)
                new WebApiHeader { RetryAfter = eex.RetryAfter }.ApplyTo(this, result.HttpResponse);

            // Convert and return exception as problem details
            ProblemDetails pd;
            if (eex is not null)
            {
                pd = pdf.CreateProblemDetails(result.HttpResponse.HttpContext, (int)eex.StatusCode, title: eex.Message, detail: eex.Detail);
                AddExtensions(pd.Extensions, CreateProblemDetailsExtensions(eex));
                if (eex.HasExtensions)
                    AddExtensions(pd.Extensions, eex.Extensions);

                return new ObjectResult(pd) { StatusCode = (int)eex.StatusCode };
            }
            else
            {
                var config = result.HttpResponse.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var include = Internal.GetConfigurationValue("CoreEx:AspNetCore:IncludeExceptionInProblemDetails", false, config);

                if (include)
                    pd = pdf.CreateProblemDetails(result.HttpResponse.HttpContext, (int)HttpStatusCode.InternalServerError, title: result.Exception.Message, detail: result.Exception.ToString());
                else
                    pd = pdf.CreateProblemDetails(result.HttpResponse.HttpContext, (int)HttpStatusCode.InternalServerError, title: new UnexpectedInternalException().Message, detail: null);

                AddExtensions(pd.Extensions, CreateProblemDetailsExtensions(null));
                return new ObjectResult(pd) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }

        result.Headers?.ApplyTo(this, result.HttpResponse);
        return result.Content is not null
            ? new ContentResult { Content = result.Content, ContentType = result.ContentType ?? MediaTypeNames.Text.Plain, StatusCode = (int)result.StatusCode }
            : new StatusCodeResult((int)result.StatusCode);
    }
}